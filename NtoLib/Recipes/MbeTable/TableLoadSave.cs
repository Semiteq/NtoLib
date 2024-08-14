using System.IO;
using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Threading;
using NtoLib.Properties;

namespace NtoLib.Recipes.MbeTable
{
    public partial class TableControl
    {
        bool isLoadingActive = false;

        private void LoadRecipeToView()
        {
            SettingsReader settingsReader = new SettingsReader(FBConnector);

            if (!settingsReader.CheckQuality())
            {
                this.WriteStatusMessage("Ошибка чтения настроек. Нет связи, продолжение загрузки рецепта не возможно", true);
            }
            else
            {
                CommunicationSettings settings = settingsReader.ReadTableSettings();

                int num = -1;
                if (settings._float_colum_num > 0)
                    num = (int)settings._FloatAreaSize / 2 / settings._float_colum_num;
                if (settings._int_colum_num > 0)
                {
                    if (num < 0)
                        num = (int)settings._IntAreaSize / settings._int_colum_num;
                    else if ((int)settings._IntAreaSize / settings._int_colum_num < num)
                        num = (int)settings._IntAreaSize / settings._int_colum_num;
                }
                if (settings._bool_colum_num > 0)
                {
                    if (num < 0)
                        num = (int)settings._BoolAreaSize * 16 / settings._bool_colum_num;
                    else if ((int)settings._BoolAreaSize * 16 / settings._bool_colum_num < num)
                        num = (int)settings._BoolAreaSize * 16 / settings._bool_colum_num;
                }
                if (num < 0)
                    this.WriteStatusMessage("Описание не загружено или ошибки при загрузки описания", true);
                else if (num == 0)
                {
                    this.WriteStatusMessage("Не выделены отдельные области памяти", true);
                }
                else
                {
                    List<RecipeLine> recipeLineList = new List<RecipeLine>();
                    List<RecipeLine> recipe;
                    try
                    {
                        recipe = ReadRecipeFromFile();
                    }
                    catch (Exception ex)
                    {
                        this.WriteStatusMessage(ex.Message, true);
                        return;
                    }

                    if (num < recipe.Count)
                    {
                        this.WriteStatusMessage("Слишком длинный рецепт, загрузка не возможна", true);
                        recipeLineList = (List<RecipeLine>)null;
                    }
                    else
                    {
                        PLC_Communication plcCommunication = new PLC_Communication();

                        plcCommunication.WriteRecipeToPlc(recipe, settings);

                        Thread.Sleep(200);

                        var recipeFromPlc = plcCommunication.LoadRecipeFromPlc(settings);

                        if (CompareRecipes(recipe, recipeFromPlc))
                            WriteStatusMessage("Рецепт успешно загружен в контроллер", false);
                        else
                            WriteStatusMessage("Рецепт загружен в контроллер. НО ОТЛИЧАЕТСЯ!!!", true);

                        dataGridView1.Rows.Clear();
                        _tableData.Clear();

                        foreach (RecipeLine line in recipe)
                            AddLineToRecipe(line, true);

                        RefreshTable();
                    }
                }
            }
        }

        /// <summary>
        /// Загружает рецепт из ПЛК и отображает его в таблице
        /// </summary>
        private bool TryLoadRecipeFromPlc()
        {
            SettingsReader settingsReader = new SettingsReader(FBConnector);
            CommunicationSettings commSettings = settingsReader.ReadTableSettings();
            PLC_Communication plcCommunication = new PLC_Communication();
            List<RecipeLine> recipe = plcCommunication.LoadRecipeFromPlc(commSettings);

            if(recipe == null)
                return false;

            dataGridView1.Rows.Clear();
            _tableData.Clear();

            foreach(RecipeLine line in recipe)
                AddLineToRecipe(line, true);

            RefreshTable();
            return true;
        }

        private bool CompareRecipes(List<RecipeLine> recipe1, List<RecipeLine> recipe2)
        {
            if (recipe1.Count != recipe2.Count)
                return false;

            for (int i = 0; i < recipe1.Count; i++)
            {
                if (recipe1[i].GetCells[0].GetValue() != recipe2[i].GetCells[0].GetValue())
                    return false;

                if (recipe1[i].GetNumber() != recipe2[i].GetNumber())
                    return false;

                if (recipe1[i].GetSetpoint() != recipe2[i].GetSetpoint())
                    return false;

                if (recipe1[i].GetTime() != recipe2[i].GetTime())
                    return false;
            }
            return true;
        }


        private void LoadRecipeToEdit()
        {
            List<RecipeLine> reserveTableData = _tableData;
            
            try
            {
                _tableData.Clear();
                _tableData = ReadRecipeFromFile();
                FillCells(_tableData);
            }
            catch (Exception ex)
            {
                WriteStatusMessage(ex.Message, true);
                _tableData = reserveTableData;
                return;
            }
        }

        private void FillCells(List<RecipeLine> data)
        {
            isLoadingActive = true;
            this.dataGridView1.Rows.Clear();
            foreach (RecipeLine recipeLine in data)
            {
                int index = this.dataGridView1.Rows.Add(new DataGridViewRow());
                dataGridView1.Rows[index].HeaderCell.Value = (object)(index + 1).ToString();
                dataGridView1.Rows[index].Height = ROW_HEIGHT;

                for (int i = 0; i < RecipeLine.ColumnCount; i++)
                {
                    object value = (object)recipeLine.GetCells[i].StringValue;
                    dataGridView1.Rows[index].Cells[i].Value = value;
                }
                BlockCells(index); 
            }
            RefreshTable();
            isLoadingActive = false;
        }

        private List<RecipeLine> ReadRecipeFromFile()
        {
            RecipeLineParser parser = new RecipeLineParser();
            List<RecipeLine> recipeLineList = new List<RecipeLine>();

            using (Stream stream = (Stream)new FileStream(this.openFileDialog1.FileName, FileMode.OpenOrCreate))
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    bool flag = true;
                    string fileline;
                    while ((fileline = streamReader.ReadLine()) != null)
                    {
                        if (flag)
                        {
                            flag = false;
                        }
                        else
                        {
                            try
                            {
                                RecipeLine recipeLine = parser.Parse(fileline); ;
                                recipeLineList.Add(recipeLine);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Ошибка при разборе строки " + (recipeLineList.Count + 1).ToString() + " : " + ex.Message);
                            }
                        }
                    }
                }
            }
            this.WriteStatusMessage("Данные загружены из файла " + this.openFileDialog1.FileName, false);
            return recipeLineList;
        }

        private void ClickButton_Open(object sender, EventArgs e)
        {
            if (this.FBConnector.DesignMode || openFileDialog1.ShowDialog() != DialogResult.OK)
                return;
            saveFileDialog1.InitialDirectory = openFileDialog1.InitialDirectory;

            if (_tableType == TableMode.View)
                LoadRecipeToView();
            else
                LoadRecipeToEdit();
        }
        private void ClickButton_Save(object sender, EventArgs e)
        {
            if (this.FBConnector.DesignMode)
                return;
            if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                using (Stream stream = (Stream)new FileStream(saveFileDialog1.FileName, FileMode.Create))
                {
                    using (StreamWriter streamWriter = new StreamWriter(stream))
                    {
                        string str1 = "";
                        bool flag1 = true;
                        foreach (TableColumn colum in columns)
                        {
                            if (!flag1)
                                str1 += ";";
                            flag1 = false;
                            str1 += colum.Name;
                        }
                        streamWriter.WriteLine(str1);
                        foreach (RecipeLine recipeLine in _tableData)
                        {
                            bool flag2 = true;
                            string str2 = "";
                            foreach (TCell cell in recipeLine.GetCells)
                            {
                                if (!flag2)
                                    str2 += ";";
                                flag2 = false;
                                str2 += cell.StringValue;
                            }
                            streamWriter.WriteLine(str2);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteStatusMessage(ex.Message, true);
                return;
            }
            this.WriteStatusMessage("Данные сохранены в файл " + this.saveFileDialog1.FileName, false);
        }
    }
}