using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.Actions.TableLines;

namespace NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime
{
    internal static class RecipeTimeManager
    {
        private static List<RecipeLine> _tableData;
        private static DataGridView _dataGridView;
        private static List<FlattenedRecipeLine> _flattenedData;

        public static TimeSpan TotalTime { get; private set; }

        /// <summary>
        /// Sets the data for calculations.
        /// </summary>
        public static void SetData(List<RecipeLine> tableData, DataGridView dataGridView)
        {
            _tableData = tableData;
            _dataGridView = dataGridView;
            _flattenedData = FlattenRecipe();
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

            var time = TimeSpan.Zero;
            _tableData[0].Time = 0f;
            UpdateDataGridView(0, TimeSpan.Zero);

            for (var rowIndex = 0; rowIndex < _tableData.Count; rowIndex++)
            {
                var recipeLine = _tableData[rowIndex];

                time += recipeLine switch
                {
                    EndFor_Loop endLoop => TimeSpan.FromSeconds(CalculateCycleTime(endLoop, rowIndex)),
                    _ when recipeLine.ActionTime == ActionTime.TimeSetpoint => TimeSpan.FromSeconds(recipeLine.Duration),
                    _ => TimeSpan.Zero
                };

                if (rowIndex != _tableData.Count - 1)
                {
                    _tableData[rowIndex + 1].Time = (float)time.TotalSeconds;
                    UpdateDataGridView(rowIndex + 1, time);
                }
            }

            TotalTime = time;
        }

        /// <summary>
        /// Gets the process time for the current row.
        /// </summary>
        /// <returns>Process time for current row</returns>
        public static TimeSpan GetRowTime(int originalIndex, int depth1, int depth2, int depth3)
        {
            if (_flattenedData == null) return TimeSpan.Zero;

            var row = _flattenedData.Find(r => r.OriginalIndex == originalIndex && r.Depth1 == depth1 && r.Depth2 == depth2 && r.Depth3 == depth3);
            return row?.ExecutionTime ?? TimeSpan.Zero;
        }

        /// <summary>
        /// Calculates the cycle time for EndFor_Loop.
        /// </summary>
        private static float CalculateCycleTime(EndFor_Loop endLoop, int rowIndex)
        {
            var cycleStartIndex = FindCycleStart(rowIndex);
            return cycleStartIndex == -1
                ? (float)0
                : (endLoop.Time - _tableData[cycleStartIndex].Time) * (_tableData[cycleStartIndex].Setpoint - 1);
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
            var tabulateLevel = 1;
            for (var i = endIndex - 1; i >= 0; i--)
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

        /// <summary>
        /// Processes a list of `RecipeLine` objects to create a flattened list of `FlattenedRecipeLine` objects.
        /// It iterates through the `RecipeLine` objects, maintaining depth counters for nested loops, and calculates
        /// the execution time for each line.
        /// </summary>
        /// <returns>List of `FlattenedRecipeLine` objects, each containing the original index, absolute execution time,
        /// depth levels, and execution time for the corresponding `RecipeLine`</returns>
        private static List<FlattenedRecipeLine> FlattenRecipe()
        {
            var flattened = new List<FlattenedRecipeLine>();
            var time = TimeSpan.Zero;
            var loopStack = new Stack<(int startIndex, int iterations, int currentIteration)>();

            for (var i = 0; i < _tableData.Count; i++)
            {
                var line = _tableData[i];

                if (line is For_Loop forLoop)
                {
                    loopStack.Push((i, (int)forLoop.Duration, 1));
                }

                var depth1 = loopStack.Count > 0 ? loopStack.Peek().currentIteration : 0;
                var depth2 = loopStack.Count > 1 ? loopStack.ToArray()[1].currentIteration : 0;
                var depth3 = loopStack.Count > 2 ? loopStack.ToArray()[2].currentIteration : 0;

                var executionTime = line.ActionTime == ActionTime.TimeSetpoint ? TimeSpan.FromSeconds(line.Duration) : TimeSpan.Zero;
                flattened.Add(new FlattenedRecipeLine(i, time, depth1, depth2, depth3, executionTime));
                time += executionTime;

                if (line is EndFor_Loop && loopStack.Count > 0)
                {
                    var (startIndex, iterations, currentIteration) = loopStack.Pop();
                    if (currentIteration < iterations)
                    {
                        loopStack.Push((startIndex, iterations, currentIteration + 1));
                        i = startIndex;
                    }
                }
            }
            return flattened;
        }
    }
}
