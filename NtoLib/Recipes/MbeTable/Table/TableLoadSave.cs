﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NtoLib.Properties;
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
            SettingsReader settingsReader = new(FBConnector);

            if (!settingsReader.CheckQuality())
            {
                StatusManager.WriteStatusMessage(
                    "Ошибка чтения настроек. Нет связи, продолжение загрузки рецепта не возможно", true);
            }
            else
            {
                CommunicationSettings settings = settingsReader.ReadTableSettings();

                int num = -1;
                if (settings.FloatColumNum > 0)
                    num = (int)settings.FloatAreaSize / 2 / settings.FloatColumNum;
                if (settings.IntColumNum > 0)
                {
                    if (num < 0)
                        num = (int)settings.IntAreaSize / settings.IntColumNum;
                    else if ((int)settings.IntAreaSize / settings.IntColumNum < num)
                        num = (int)settings.IntAreaSize / settings.IntColumNum;
                }

                if (settings.BoolColumNum > 0)
                {
                    if (num < 0)
                        num = (int)settings.BoolAreaSize * 16 / settings.BoolColumNum;
                    else if ((int)settings.BoolAreaSize * 16 / settings.BoolColumNum < num)
                        num = (int)settings.BoolAreaSize * 16 / settings.BoolColumNum;
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

                    if (recipe.Count == 0)
                    {
                        StatusManager.WriteStatusMessage("Ошибка при чтении рецепта", true);
                        return;
                    }


                    if (num < recipe.Count)
                    {
                        StatusManager.WriteStatusMessage("Слишком длинный рецепт, загрузка не возможна", true);
                    }
                    else
                    {
                        PlcCommunication plcCommunication = new();

                        if (!plcCommunication.CheckConnection(settings))
                        {
                            StatusManager.WriteStatusMessage("Ошибка соединения с контроллером", true);
                            return;
                        }

                        if (!plcCommunication.WriteRecipeToPlc(recipe, settings))
                        {
                            StatusManager.WriteStatusMessage("Ошибка записи рецепта в контроллер", true);
                            return;
                        }

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
                                StatusManager.WriteStatusMessage("Рецепт загружен в контроллер. НО ОТЛИЧАЕТСЯ!!!",
                                    true);
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

            PlcCommunication plcCommunication = new();


            if (!plcCommunication.CheckConnection(commSettings))
            {
                StatusManager.WriteStatusMessage("Нет соединения с ПЛК", true);
                return false;
            }

            var recipe = plcCommunication.LoadRecipeFromPlc(commSettings);

            if (recipe == null)
            {
                StatusManager.WriteStatusMessage("Не удалось загрузить рецепт из ПЛК");
                return false;
            }

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

            // not checking comments
            for (var i = 0; i < recipe1.Count - 1; i++)
            {
                var cells1 = recipe1[i].Cells;
                var cells2 = recipe2[i].Cells;

                if (cells1.Count != cells2.Count)
                    return false;

                for (var j = 0; j < cells1.Count; j++)
                {
                    if (cells1[j].GetValue() == cells2[j].GetValue()) continue;
                    Debug.WriteLine($"Cell {j} in row {i} differs: {cells1[j].GetValue()} != {cells2[j].GetValue()}");
                    return false;
                }
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


        private List<RecipeLine> ReadRecipeFromFile()
        {
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
                        var recipeLine = RecipeLineParser.Parse(line);
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

                StatusManager.WriteStatusMessage($"Данные сохранены в файл {saveFileDialog1.FileName}", false);
            }
            catch (Exception ex)
            {
                StatusManager.WriteStatusMessage($"Ошибка при сохранении: {ex.Message}", true);
            }
        }
    }
}