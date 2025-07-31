using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe
{
    // public class TableLoopManager
    // {
    //     private readonly int _maxLoopCount;
    //     private readonly DataGridView _dataGridView;
    //     private readonly ActionManager _actionManager;
    //     private readonly List<Step> _tableData;
    //
    //     public TableLoopManager(DataGridView dataGridView, List<Step> tableData, ActionManager actionManager, int maxLoopCount = 3)
    //     {
    //         _dataGridView = dataGridView ?? throw new ArgumentNullException(nameof(dataGridView));
    //         _tableData = tableData ?? throw new ArgumentNullException(nameof(tableData));
    //         _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
    //         _maxLoopCount = maxLoopCount;
    //     }
    //
    //     public bool CheckRecipeCycles(List<Step> recipe)
    //     {
    //         var cycleDepth = 0;
    //         foreach (var recipeLine in recipe)
    //         {
    //             if (cycleDepth > _maxLoopCount)
    //                 return false;
    //
    //             var actionId = recipeLine.GetProperty(ColumnKey.Action).GetValue<int>();
    //             _actionManager.GetActionEntryById(actionId, out var actionEntry, out _);
    //
    //             if (actionEntry == _actionManager.ForLoop)
    //             {
    //                 recipeLine.NestingLevel = cycleDepth;
    //                 cycleDepth++;
    //             }
    //             else if (actionEntry == _actionManager.EndForLoop)
    //             {
    //                 cycleDepth--;
    //                 if (cycleDepth < 0)
    //                     return false;
    //                 recipeLine.NestingLevel = cycleDepth;
    //             }
    //             else
    //             {
    //                 recipeLine.NestingLevel = cycleDepth;
    //             }
    //         }
    //
    //         Tabulate();
    //         return cycleDepth == 0;
    //     }
    //
    //     public void Tabulate()
    //     {
    //         var cellStyle = new DataGridViewCellStyle
    //         {
    //             BackColor = Color.Coral,
    //             ForeColor = Color.Black,
    //             SelectionBackColor = Color.Chocolate
    //         };
    //
    //         for (var i = 0; i < _tableData.Count; i++)
    //         {
    //             var tabulatorString = string.Empty;
    //             var tabulatorLevel = 0;
    //             
    //             for (tabulatorLevel = 0; tabulatorLevel < _tableData[i].NestingLevel; tabulatorLevel++)
    //                 tabulatorString += "\t";
    //
    //             _dataGridView.Rows[i].HeaderCell.Value = tabulatorString + (i + 1).ToString();
    //
    //             _dataGridView.Rows[i].Cells[0].Style.BackColor = tabulatorLevel switch
    //             {
    //                 0 => Color.White,
    //                 1 => Color.LightBlue,
    //                 2 => Color.LightSkyBlue,
    //                 3 => Color.DodgerBlue,
    //                 _ => _dataGridView.Rows[i].Cells[0].Style.BackColor
    //             };
    //         }
    //     }
    //
    //     public int FindCycleStart(int endIndex)
    //     {
    //         var level = 1;
    //         for (var i = endIndex - 1; i >= 0; i--)
    //         {
    //             _tableData[i].GetPropertyValue(ColumnKey.Action, out int actionId);
    //             _actionManager.GetActionEntryById(actionId, out var actionEntry, out _);
    //
    //             if (actionEntry == _actionManager.EndForLoop)
    //             {
    //                 level++;
    //             }
    //             else if (actionEntry == _actionManager.ForLoop)
    //             {
    //                 level--;
    //             }
    //
    //             if (level == 0)
    //                 return i;
    //         }
    //         return -1;
    //     }
    //
    //     public int FindCycleEnd(int startIndex)
    //     {
    //         var level = 1;
    //         for (var i = startIndex + 1; i < _tableData.Count; i++)
    //         {
    //             _tableData[i].GetPropertyValue(ColumnKey.Action, out int actionId);
    //             _actionManager.GetActionEntryById(actionId, out var actionEntry, out _);
    //
    //             if (actionEntry == _actionManager.ForLoop)
    //             {
    //                 level++;
    //             }
    //             else if (actionEntry == _actionManager.EndForLoop)
    //             {
    //                 level--;
    //             }
    //
    //             if (level == 0)
    //                 return i;
    //         }
    //         return -1;
    //     }
    // }
}