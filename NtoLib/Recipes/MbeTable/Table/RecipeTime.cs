using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.TableLines;

namespace NtoLib.Recipes.MbeTable
{
    internal sealed class RecipeTime
    {
        private static List<RecipeLine> _tableData;
        private static DataGridView _dataGridView;
        public static TimeSpan TotalTime { get; private set; }

        /// <summary>
        /// Sets the data for calculations.
        /// </summary>
        public static void SetData(List<RecipeLine> tableData, DataGridView dataGridView)
        {
            _tableData = tableData;
            _dataGridView = dataGridView;
        }

        /// <summary>
        /// Recalculates the recipe execution time and updates the DataGridView.
        /// </summary>
        /// <returns>Total execution time.</returns>
        public static void Recalculate()
        {
            if (_tableData == null || _tableData.Count == 0)
            {
                TotalTime = TimeSpan.Zero;
                return;
            }

            TimeSpan time = TimeSpan.Zero;
            _tableData[0].CycleTime = 0f;
            UpdateDataGridView(0, TimeSpan.Zero);

            for (var rowIndex = 0; rowIndex < _tableData.Count; rowIndex++)
            {
                var recipeLine = _tableData[rowIndex];

                time += recipeLine switch
                {
                    EndFor_Loop endLoop => TimeSpan.FromSeconds(CalculateCycleTime(endLoop, rowIndex)),
                    _ when recipeLine.ActionTime == ActionTime.TimeSetpoint => TimeSpan.FromSeconds(recipeLine.GetTime()),
                    _ => TimeSpan.Zero
                };

                if (rowIndex != _tableData.Count - 1)
                {
                    _tableData[rowIndex + 1].CycleTime = (float)time.TotalSeconds;
                    UpdateDataGridView(rowIndex + 1, time);
                }
            }

            TotalTime = time;
        }
        
        /// <summary>
        /// Gets the process time for the current row.
        /// </summary>
        /// <returns>Process time for current row</returns>
        public static TimeSpan GetRowTime(int rowIndex)
        {
            if (_tableData == null || rowIndex < 0 || rowIndex >= _tableData.Count)
                return TimeSpan.Zero;

            return _tableData[rowIndex].ActionTime == ActionTime.TimeSetpoint
                ? TimeSpan.FromSeconds(_tableData[rowIndex].GetTime())
                : TimeSpan.Zero;
        }

        /// <summary>
        /// Calculates the cycle time for EndFor_Loop.
        /// </summary>
        private static float CalculateCycleTime(EndFor_Loop endLoop, int rowIndex)
        {
            int cycleStartIndex = FindCycleStart(rowIndex);
            if (cycleStartIndex == -1) return 0;

            return (endLoop.CycleTime - _tableData[cycleStartIndex].CycleTime) * (_tableData[cycleStartIndex].GetSetpoint() - 1);
        }

        /// <summary>
        /// Updates the DataGridView with calculated values.
        /// </summary>
        private static void UpdateDataGridView(int rowIndex, TimeSpan time)
        {
            if (_dataGridView == null) return;
            _dataGridView.Rows[rowIndex].Cells[Params.RecipeTimeIndex].Value = time.ToString("hh\\:mm\\:ss\\.ff");
        }

        /// <summary>
        /// Finds the index of the start of the For_Loop loop given its end EndFor_Loop.
        /// </summary>
        private static int FindCycleStart(int endIndex)
        {
            int tabulateLevel = 1;
            for (int i = endIndex - 1; i >= 0; i--)
            {
                if (_tableData[i] is EndFor_Loop)
                    tabulateLevel++;
                else if (_tableData[i] is For_Loop)
                    tabulateLevel--;

                if (tabulateLevel == 0)
                    return i;
            }
            return -1;
        }
    }
}
