using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.PLC;
using NtoLib.Recipes.MbeTable.RecipeLines;
using NtoLib.Recipes.MbeTable.Table;

namespace NtoLib.Recipes.MbeTable
{
    public partial class TableControl
    {
        private bool isLoadingActive = false;

        private void LoadRecipeToView(string filePath)
        {

            var settingsReader = new SettingsReader(FBConnector);
            var recipeComparator = new RecipeComparator();

            if (!TryGetSettings(settingsReader, out var settings))
                return;

            var maxRows = CalculateMaxRows(settings);
            if (maxRows <= 0)
                return;

            List<RecipeLine> recipe;

            try
            {
                recipe = recipeFileReader.Read(filePath);
            }
            catch (Exception ex)
            {
                statusManager.WriteStatusMessage(ex.Message, true);
                Debug.WriteLine(ex);
                return;
            }

            if (recipe == null || recipe.Count == 0)
                return;

            if (maxRows < recipe.Count)
            {
                statusManager.WriteStatusMessage("Слишком длинный рецепт, загрузка не возможна", true);
                return;
            }

            if (!TryWriteToPlcAndVerifyRecipe(recipe, settings, recipeComparator))
                return;

            UpdateTableView(recipe);
        }

        private bool TryGetSettings(SettingsReader settingsReader, out CommunicationSettings settings)
        {
            settings = null;
            if (!settingsReader.CheckQuality())
            {
                statusManager.WriteStatusMessage(
                    "Ошибка чтения настроек. Нет связи, продолжение загрузки рецепта не возможно", true);
                return false;
            }

            settings = settingsReader.ReadTableSettings();
            return true;
        }

        private int CalculateMaxRows(CommunicationSettings settings)
        {
            //todo: add check when new line is added
            int maxRows = -1;

            if (settings.FloatColumNum > 0)
                maxRows = (int)settings.FloatAreaSize / 2 / settings.FloatColumNum;

            if (settings.IntColumNum > 0)
                maxRows = maxRows < 0
                    ? (int)settings.IntAreaSize / settings.IntColumNum
                    : Math.Min(maxRows, (int)settings.IntAreaSize / settings.IntColumNum);

            if (settings.BoolColumNum > 0)
                maxRows = maxRows < 0
                    ? (int)settings.BoolAreaSize * 16 / settings.BoolColumNum
                    : Math.Min(maxRows, (int)settings.BoolAreaSize * 16 / settings.BoolColumNum);

            if (maxRows < 0)
                statusManager.WriteStatusMessage("Описание не загружено или ошибки при загрузки описания", true);
            else if (maxRows == 0)
                statusManager.WriteStatusMessage("Не выделены отдельные области памяти", true);

            return maxRows;
        }

        private bool TryWriteToPlcAndVerifyRecipe(List<RecipeLine> recipe, CommunicationSettings settings,
            RecipeComparator recipeComparator)
        {
            if (!plcCommunication.CheckConnection(settings))
            {
                statusManager.WriteStatusMessage("Ошибка соединения с контроллером", true);
                return false;
            }

            if (!plcCommunication.WriteRecipeToPlc(recipe, settings))
            {
                statusManager.WriteStatusMessage("Ошибка записи рецепта в контроллер", true);
                return false;
            }

            Thread.Sleep(200);

            List<RecipeLine> recipeFromPlc;

            try
            {
                recipeFromPlc = plcCommunication.LoadRecipeFromPlc(settings);
            }
            catch (Exception ex)
            {
                statusManager.WriteStatusMessage($"Рецепт не удалось загрузить в контроллер: {ex}", true);
                return false;
            }

            var isMatch = recipeComparator.Compare(recipe, recipeFromPlc);
            statusManager.WriteStatusMessage(isMatch
                ? "Рецепт успешно загружен в контроллер"
                : "Рецепт загружен в контроллер. НО ОТЛИЧАЕТСЯ!!!", !isMatch);

            return true;
        }

        private void UpdateTableView(List<RecipeLine> recipe)
        {
            dataGridView1.Rows.Clear();
            _tableData.Clear();

            foreach (var line in recipe)
                AddLineToRecipe(line, true);

            RefreshTable();
        }

        /// <summary>
        /// Loads the recipe from the PLC and displays it in the table.
        /// </summary>
        private bool TryLoadRecipeFromPlc()
        {
            SettingsReader settingsReader = new(FBConnector);
            var commSettings = settingsReader.ReadTableSettings();

            if (!plcCommunication.CheckConnection(commSettings))
            {
                statusManager.WriteStatusMessage("Нет соединения с ПЛК", true);
                return false;
            }

            List<RecipeLine> recipe;

            try
            {
                recipe = plcCommunication.LoadRecipeFromPlc(commSettings);

            }
            catch
            {
                statusManager.WriteStatusMessage("Не удалось загрузить рецепт из ПЛК");
                return false;
            }

            dataGridView1.Rows.Clear();
            _tableData.Clear();

            foreach (RecipeLine line in recipe)
                AddLineToRecipe(line, true);

            RefreshTable();
            return true;
        }

        private void LoadRecipeToEdit(string filePath)
        {
            try
            {
                var fileData = recipeFileReader.Read(filePath);
                
                _tableData.Clear();
                _tableData = fileData;
                FillCells(_tableData);
                statusManager.WriteStatusMessage($"Загружены данные из файла: {filePath}");
            }
            catch (Exception ex)
            {
                statusManager.WriteStatusMessage(ex.Message, true); 
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

                var action = recipeLine.Cells[Params.ActionIndex].StringValue;

                for (var colIndex = 0; colIndex < Params.ColumnCount; colIndex++)
                {
                    var cellValue = recipeLine.Cells[colIndex].StringValue;
                    var cell = row.Cells[colIndex];

                    cell.Value = cellValue;

                    // Number column combo box update
                    if (colIndex == Params.ActionTargetIndex && cell is DataGridViewComboBoxCell comboBoxCell &&
                        cellValue != null)
                    {
                        UpdateEnumDropDown(comboBoxCell, cellValue);
                    }
                }

                BlockCells(rowIndex);
            }

            RefreshTable();
            isLoadingActive = false;
        }


        private void ClickButton_Open(object sender, EventArgs e)
        {
            if (this.FBConnector.DesignMode || openFileDialog1.ShowDialog() != DialogResult.OK)
                return;
            
            if (_tableType == TableMode.View)
                LoadRecipeToView(openFileDialog1.FileName);
            else
                LoadRecipeToEdit(openFileDialog1.FileName);
        }

        private void ClickButton_Save(object sender, EventArgs e)
        {
            if (FBConnector.DesignMode) return;
            
            if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                return;
            
            try
            {
                recipeFileWriter.Write(_tableData, saveFileDialog1.FileName);
                statusManager.WriteStatusMessage($"Файл успешно сохранен: {saveFileDialog1.FileName}");
            }
            catch (Exception ex)
            {
                statusManager.WriteStatusMessage($"{ex.Message}");
            }
        }
    }
}