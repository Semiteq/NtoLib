using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.RecipeLines;
using NtoLib.Recipes.MbeTable.Table;

namespace NtoLib.Recipes.MbeTable
{
    public partial class TableControl
    {
        private bool isLoadingActive = false;

        private void LoadRecipeToView()
        {
            SettingsReader settingsReader = new(FBConnector);

            if (!settingsReader.CheckQuality())
            {
                StatusManager.WriteStatusMessage("Ошибка чтения настроек. Нет связи, продолжение загрузки рецепта не возможно", true);
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
                    StatusManager.WriteStatusMessage("Описание не загружено или ошибки при загрузки описания", true);
                else if (num == 0)
                {
                    StatusManager.WriteStatusMessage("Не выделены отдельные области памяти", true);
                }
                else
                {
                    List<RecipeLine> recipeLineList = new();
                    List<RecipeLine> recipe;
                    try
                    {
                        recipe = ReadRecipeFromFile();
                    }
                    catch (Exception ex)
                    {
                        StatusManager.WriteStatusMessage(ex.Message, true);
                        return;
                    }

                    if (num < recipe.Count)
                    {
                        StatusManager.WriteStatusMessage("Слишком длинный рецепт, загрузка не возможна", true);

                    }
                    else
                    {
                        PLC_Communication plcCommunication = new();

                        plcCommunication.WriteRecipeToPlc(recipe, settings);

                        Thread.Sleep(200);

                        dataGridView1.Rows.Clear();
                        _tableData.Clear();

                        var recipeFromPlc = plcCommunication.LoadRecipeFromPlc(settings);
                        if (recipeFromPlc == null)
                        {
                            StatusManager.WriteStatusMessage("Рецепт не удалось загрузить в контроллер", true);
                            return;
                        }
                        else
                        {
                            if (CompareRecipes(recipe, recipeFromPlc))
                                StatusManager.WriteStatusMessage("Рецепт успешно загружен в контроллер");
                            else
                                StatusManager.WriteStatusMessage("Рецепт загружен в контроллер. НО ОТЛИЧАЕТСЯ!!!", true);
                        }

                        foreach (RecipeLine line in recipe)
                            AddLineToRecipe(line, true);

                        RefreshTable();
                    }
                }
            }
        }

        /// <summary>
        /// Loads the recipe from the PLC and displays it in the table.
        /// </summary>
        private bool TryLoadRecipeFromPlc()
        {
            SettingsReader settingsReader = new(FBConnector);
            var commSettings = settingsReader.ReadTableSettings();
            PLC_Communication plcCommunication = new();
            var recipe = plcCommunication.LoadRecipeFromPlc(commSettings);

            if (recipe == null)
                return false;

            dataGridView1.Rows.Clear();
            _tableData.Clear();

            foreach (RecipeLine line in recipe)
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
            var reserveTableData = _tableData;

            try
            {
                _tableData.Clear();
                _tableData = ReadRecipeFromFile();
                FillCells(_tableData);
            }
            catch (Exception ex)
            {
                StatusManager.WriteStatusMessage(ex.Message, true);
                _tableData = reserveTableData;
            }
        }

        private void FillCells(List<RecipeLine> data)
        {
            isLoadingActive = true;
            dataGridView1.Rows.Clear();

            foreach (var recipeLine in data)
            {
                var rowIndex = dataGridView1.Rows.Add(new DataGridViewRow());
                var row = dataGridView1.Rows[rowIndex];

                row.HeaderCell.Value = (rowIndex + 1).ToString();
                row.Height = ROW_HEIGHT;

                var action = recipeLine.GetCells[Params.ActionIndex].StringValue;

                for (var colIndex = 0; colIndex < Params.ColumnCount; colIndex++)
                {
                    var cellValue = recipeLine.GetCells[colIndex].StringValue;
                    var cell = row.Cells[colIndex];

                    cell.Value = cellValue;

                    // Number column combo box update
                    if (colIndex == Params.ActionTargetIndex && cell is DataGridViewComboBoxCell comboBoxCell && cellValue != null)
                    {
                        UpdateEnumDropDown(comboBoxCell, cellValue);
                    }
                }

                BlockCells(rowIndex);
            }

            RefreshTable();
            isLoadingActive = false;
        }


        private List<RecipeLine> ReadRecipeFromFile()
        {
            var parser = new RecipeLineParser();
            var recipeLineList = new List<RecipeLine>();

            try
            {
                using var stream = new FileStream(openFileDialog1.FileName, FileMode.OpenOrCreate);
                using var streamReader = new StreamReader(stream);

                var isHeader = true;

                while (streamReader.ReadLine() is { } line)
                {
                    if (isHeader)
                    {
                        isHeader = false;
                        continue;
                    }

                    try
                    {
                        var recipeLine = parser.Parse(line);
                        recipeLineList.Add(recipeLine);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Ошибка при разборе строки {recipeLineList.Count + 1}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusManager.WriteStatusMessage($"Ошибка при загрузке файла: {ex.Message}", true);
                throw;
            }

            StatusManager.WriteStatusMessage($"Данные загружены из файла {openFileDialog1.FileName}", false);
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
            if (FBConnector.DesignMode) return;
            if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;

            try
            {
                using (var stream = new FileStream(saveFileDialog1.FileName, FileMode.Create))
                using (var streamWriter = new StreamWriter(stream))
                {
                    // Writing headers
                    var columnHeaders = string.Join(";", columns.Select(column => column.Name));
                    streamWriter.WriteLine(columnHeaders);

                    // Writing lines
                    foreach (var recipeLine in _tableData)
                    {
                        var cells = recipeLine.GetCells.ToList();
                        var rowData = new List<string>();
                        var currentCommand = cells[Params.ActionIndex].StringValue;
                        var action = ActionManager.GetTargetAction(currentCommand);

                        for (var i = 0; i < cells.Count; i++)
                        {

                            var cellValue = cells[i].StringValue;

                            if (i == Params.ActionIndex)
                            {
                                cellValue = ActionManager.GetActionIdByCommand(cellValue).ToString();
                            }

                            if (i == Params.ActionTargetIndex && action != ActionType.Unspecified)
                            {
                                try
                                {
                                    cellValue = GrowthList.GetActionTypeByName(cellValue, currentCommand).ToString();
                                }
                                catch (KeyNotFoundException)
                                {
                                    cellValue = "";
                                }
                            }

                            rowData.Add(cellValue);
                        }

                        streamWriter.WriteLine(string.Join(";", rowData));
                    }
                }

                StatusManager.WriteStatusMessage($"Данные сохранены в файл {saveFileDialog1.FileName}", false);
            }
            catch (Exception ex)
            {
                StatusManager.WriteStatusMessage($"Ошибка при сохранении: {ex.Message}", true);
            }
        }
    }
}