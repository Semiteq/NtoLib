// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.Threading;
// using System.Threading.Tasks;
// using System.Windows.Forms;
// using NtoLib.Recipes.MbeTable.IO;
// using NtoLib.Recipes.MbeTable.PLC;
// using NtoLib.Recipes.MbeTable.Recipe;
// using NtoLib.Recipes.MbeTable.Recipe.StepManager;
// using NtoLib.Recipes.MbeTable.RecipeLines;
// using NtoLib.Recipes.MbeTable.Table;
// using NtoLib.Recipes.MbeTable.Table.UI.StatusManager;
//
// namespace NtoLib.Recipes.MbeTable.Managers.Contracts
// {
//     /// <summary>
//     /// Управляет загрузкой и сохранением рецептов
//     /// </summary>
//     public class TableFileManager
//     {
//         private readonly DataGridView _dataGridView;
//         private readonly List<Step> _tableData;
//         private readonly StatusManager _statusManager;
//         private readonly PlcCommunication _plcCommunication;
//         private readonly RecipeFileReader _recipeFileReader;
//         private readonly RecipeFileWriter _recipeFileWriter;
//         private readonly TableLineManager _lineManager;
//         private readonly TableMode _tableMode;
//         private readonly CommunicationSettings _communicationSettings;
//         
//         private bool _isLoadingActive = false;
//
//         public TableFileManager(DataGridView dataGridView, List<Step> tableData,
//             StatusManager statusManager, PlcCommunication plcCommunication,
//             RecipeFileReader recipeFileReader, RecipeFileWriter recipeFileWriter,
//             TableLineManager lineManager, TableMode tableMode, CommunicationSettings communicationSettings)
//         {
//             _dataGridView = dataGridView;
//             _tableData = tableData;
//             _statusManager = statusManager;
//             _plcCommunication = plcCommunication;
//             _recipeFileReader = recipeFileReader;
//             _recipeFileWriter = recipeFileWriter;
//             _lineManager = lineManager;
//             _tableMode = tableMode;
//             _communicationSettings = communicationSettings;
//         }
//
//         public void LoadRecipe(string filePath)
//         {
//             if (_tableMode == TableMode.View)
//                 LoadRecipeToView(filePath);
//             else
//                 LoadRecipeToEdit(filePath);
//         }
//
//         public void SaveRecipe(string filePath)
//         {
//             try
//             {
//                 _recipeFileWriter.Write(_tableData, filePath);
//                 _statusManager.WriteStatusMessage($"Файл успешно сохранен: {filePath}", StatusMessage.Info);
//             }
//             catch (Exception ex)
//             {
//                 _statusManager.WriteStatusMessage($"{ex.Message}", StatusMessage.Info);
//             }
//         }
//
//         public async Task<bool> TryLoadRecipeFromPlc()
//         {
//             try
//             {
//                 if (!await _plcCommunication.CheckConnectionWithRetryAsync(_communicationSettings))
//                 {
//                     _statusManager.WriteStatusMessage("Ошибка соединения с контроллером", StatusMessage.Error);
//                     return false;
//                 }
//             }
//             catch (System.IO.IOException ex)
//             {
//                 _statusManager.WriteStatusMessage($"Ошибка соединения с ПЛК: {ex.Message}", StatusMessage.Error);
//                 return false;
//             }
//
//             List<Step> recipe;
//
//             try
//             {
//                 recipe = _plcCommunication.LoadRecipeFromPlc(_communicationSettings);
//             }
//             catch
//             {
//                 _statusManager.WriteStatusMessage("Не удалось загрузить рецепт из ПЛК");
//                 return false;
//             }
//
//             _dataGridView.Rows.Clear();
//             _tableData.Clear();
//
//             foreach (Step line in recipe)
//                 _lineManager.AddLineToRecipe(line, true);
//
//             _lineManager.RefreshTable();
//             return true;
//         }
//
//         private async Task LoadRecipeToView(string filePath)
//         {
//             var recipeComparator = new RecipeComparator();
//
//             var maxRows = CalculateMaxRows(_communicationSettings);
//             if (maxRows <= 0)
//                 return;
//
//             List<Step> recipe;
//
//             try
//             {
//                 recipe = _recipeFileReader.Read(filePath);
//             }
//             catch (Exception ex)
//             {
//                 _statusManager.WriteStatusMessage(ex.Message, true);
//                 Debug.WriteLine(ex);
//                 return;
//             }
//
//             if (recipe == null || recipe.Count == 0)
//                 return;
//
//             if (maxRows < recipe.Count)
//             {
//                 _statusManager.WriteStatusMessage("Слишком длинный рецепт, загрузка не возможна", true);
//                 return;
//             }
//
//             if (!await TryWriteToPlcAndVerifyRecipe(recipe, _communicationSettings, recipeComparator))
//                 return;
//
//             UpdateTableView(recipe);
//         }
//
//         private void LoadRecipeToEdit(string filePath)
//         {
//             try
//             {
//                 var fileData = _recipeFileReader.Read(filePath);
//                 
//                 _tableData.Clear();
//                 _tableData.AddRange(fileData);
//                 FillCells(_tableData);
//                 _statusManager.WriteStatusMessage($"Загружены данные из файла: {filePath}");
//             }
//             catch (Exception ex)
//             {
//                 _statusManager.WriteStatusMessage(ex.Message, true); 
//             }
//         }
//
//         private void FillCells(List<Step> data)
//         {
//             _isLoadingActive = true;
//             _dataGridView.Rows.Clear();
//
//             foreach (var recipeLine in data)
//             {
//                 var rowIndex = _dataGridView.Rows.Add(new DataGridViewRow());
//                 var row = _dataGridView.Rows[rowIndex];
//
//                 row.HeaderCell.Value = (rowIndex + 1).ToString();
//                 row.Height = 32; // ROW_HEIGHT
//
//                 for (var colIndex = 0; colIndex < Params.ColumnCount; colIndex++)
//                 {
//                     var cellValue = recipeLine.Cells[colIndex].StringValue;
//                     var cell = row.Cells[colIndex];
//
//                     cell.Value = cellValue;
//
//                     if (colIndex == Params.ActionTargetIndex && cell is DataGridViewComboBoxCell comboBoxCell &&
//                         cellValue != null)
//                     {
//                         UpdateEnumDropDown(comboBoxCell, cellValue);
//                     }
//                 }
//
//                 // BlockCells через lineManager
//             }
//
//             _lineManager.RefreshTable();
//             _isLoadingActive = false;
//         }
//
//         private int CalculateMaxRows(CommunicationSettings settings)
//         {
//             int maxRows = -1;
//
//             if (CommunicationSettings.FloatColumNum > 0)
//                 maxRows = (int)settings.FloatAreaSize / 2 / CommunicationSettings.FloatColumNum;
//
//             if (CommunicationSettings.IntColumNum > 0)
//                 maxRows = maxRows < 0
//                     ? (int)settings.IntAreaSize / CommunicationSettings.IntColumNum
//                     : Math.Min(maxRows, (int)settings.IntAreaSize / CommunicationSettings.IntColumNum);
//
//             if (CommunicationSettings.BoolColumNum > 0)
//                 maxRows = maxRows < 0
//                     ? (int)settings.BoolAreaSize * 16 / CommunicationSettings.BoolColumNum
//                     : Math.Min(maxRows, (int)settings.BoolAreaSize * 16 / CommunicationSettings.BoolColumNum);
//
//             if (maxRows < 0)
//                 _statusManager.WriteStatusMessage("Описание не загружено или ошибки при загрузки описания", true);
//             else if (maxRows == 0)
//                 _statusManager.WriteStatusMessage("Не выделены отдельные области памяти", true);
//
//             return maxRows;
//         }
//
//         private async Task<bool> TryWriteToPlcAndVerifyRecipe(List<Step> recipe, CommunicationSettings settings,
//             RecipeComparator recipeComparator)
//         {
//             if (!(await _plcCommunication.CheckConnectionWithRetryAsync(_communicationSettings)))
//             {
//                 _statusManager.WriteStatusMessage("Ошибка соединения с контроллером", true);
//                 return false;
//             }
//
//             if (!_plcCommunication.WriteRecipeToPlc(recipe, settings))
//             {
//                 _statusManager.WriteStatusMessage("Ошибка записи рецепта в контроллер", true);
//                 return false;
//             }
//
//             Thread.Sleep(200);
//
//             List<Step> recipeFromPlc;
//
//             try
//             {
//                 recipeFromPlc = _plcCommunication.LoadRecipeFromPlc(settings);
//             }
//             catch (Exception ex)
//             {
//                 _statusManager.WriteStatusMessage($"Рецепт не удалось загрузить в контроллер: {ex}", true);
//                 return false;
//             }
//
//             var isMatch = recipeComparator.Compare(recipe, recipeFromPlc);
//             _statusManager.WriteStatusMessage(isMatch
//                 ? "Рецепт успешно загружен в контроллер"
//                 : "Рецепт загружен в контроллер. НО ОТЛИЧАЕТСЯ!!!", !isMatch);
//
//             return true;
//         }
//
//         private void UpdateTableView(List<Step> recipe)
//         {
//             _dataGridView.Rows.Clear();
//             _tableData.Clear();
//
//             foreach (var line in recipe)
//                 _lineManager.AddLineToRecipe(line, true);
//
//             _lineManager.RefreshTable();
//         }
//
//         private void UpdateEnumDropDown(DataGridViewComboBoxCell cell, object cellValue)
//         {
//             // Перенести логику из основного класса
//         }
//
//         public bool IsLoadingActive => _isLoadingActive;
//     }
// }