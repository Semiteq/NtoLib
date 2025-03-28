using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.PLC;
using NtoLib.Recipes.MbeTable.RecipeLines;
using NtoLib.Recipes.MbeTable.Table;

namespace NtoLib.Recipes.MbeTable
{
    public partial class TableControl
    {
        private bool isLoadingActive = false;

        private void LoadRecipeToView()
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
                recipe = recipeFileReader.Read();
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

            if (!TryWriteAndVerifyRecipe(recipe, settings, recipeComparator))
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

        private bool TryWriteAndVerifyRecipe(List<RecipeLine> recipe, CommunicationSettings settings,
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

        private void LoadRecipeToEdit()
        {
            var reserveTableData = _tableData;

            try
            {
                _tableData.Clear();
                _tableData = recipeFileReader.Read();
                FillCells(_tableData);
            }
            catch (Exception ex)
            {
                statusManager.WriteStatusMessage(ex.Message, true);
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
                        var cells = recipeLine.Cells.ToList();
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
                                    cellValue = ActionTarget.GetActionTypeByName(cellValue, currentCommand).ToString();
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

                statusManager.WriteStatusMessage($"Данные сохранены в файл {saveFileDialog1.FileName}", false);
            }
            catch (Exception ex)
            {
                statusManager.WriteStatusMessage($"Ошибка при сохранении: {ex.Message}", true);
            }
        }
    }
}