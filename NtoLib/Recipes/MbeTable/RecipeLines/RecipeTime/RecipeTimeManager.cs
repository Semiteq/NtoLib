#nullable enable
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.Actions.TableLines;

namespace NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime
{
    internal class RecipeTimeManager
    {
        private static List<RecipeLine>? _tableData;
        private static DataGridView? _dataGridView;
        private static List<FlattenedRecipeLine>? _flattenedData;
        
        private static DateTime _overallTimerStart;
        private static TimeSpan? _lastLoggedOverallRemaining;
        private const double CorrectionThreshold = 0.1; // Correction threshold (in seconds)
        private static bool _isRecipeRunning;
        
        public static TimeSpan TotalTime { get; private set; }

        /// <summary>
        /// Sets the data for calculations.
        /// </summary>
        public static void SetData(List<RecipeLine>? tableData, DataGridView? dataGridView)
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
                ? 0f
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
        private static List<FlattenedRecipeLine>? FlattenRecipe()
        {
            var flattened = new List<FlattenedRecipeLine>();
            var time = TimeSpan.Zero;
            var loopStack = new Stack<(int startIndex, int iterations, int currentIteration)>();

            for (var i = 0; i < _tableData.Count; i++)
            {
                var line = _tableData[i];
                var maxLoop = 1;
                
                if (line is For_Loop forLoop)
                {
                    maxLoop = (int)line.Setpoint;
                    loopStack.Push((i, maxLoop, 1));
                }
                else if (line is EndFor_Loop && loopStack.Count > 0)
                {
                    maxLoop = loopStack.Peek().iterations;
                }
                
                var depth1 = loopStack.Count > 0 ? loopStack.Peek().currentIteration : 0;
                var depth2 = loopStack.Count > 1 ? loopStack.ToArray()[1].currentIteration : 0;
                var depth3 = loopStack.Count > 2 ? loopStack.ToArray()[2].currentIteration : 0;

                var executionTime = line.ActionTime == ActionTime.TimeSetpoint
                    ? TimeSpan.FromSeconds(line.Duration)
                    : TimeSpan.Zero;
                flattened.Add(new FlattenedRecipeLine(i, time, depth1, depth2, depth3, executionTime));

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

        public static CountTimer? ManageRecipeTimer(bool isRecipeActive, CountTimer? currentTimer, TimeSpan totalRecipeTime, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<RecipeTimeManager>();

            if (!isRecipeActive)
            {
                if (_isRecipeRunning)
                {
                    currentTimer?.Stop();
                    _isRecipeRunning = false;
                    logger.LogInformation("Recipe inactive: Overall timer reset.");
                }
                return null;
            }

            if (totalRecipeTime == TimeSpan.Zero)
                return currentTimer;

            if (!_isRecipeRunning)
            {
                _overallTimerStart = DateTime.UtcNow;
                currentTimer = new CountTimer(totalRecipeTime, loggerFactory.CreateLogger<CountTimer>());
                currentTimer.Start();
                logger.LogInformation("Overall timer started with total time {TotalTime}.", totalRecipeTime);
                _isRecipeRunning = true;
            }
            else
            {
                var elapsed = DateTime.UtcNow - _overallTimerStart;
                var remaining = totalRecipeTime - elapsed;
                if (remaining > TimeSpan.Zero)
                {
                    currentTimer?.SetRemainingTime(remaining);
                }
            }
            return currentTimer;
        }


        /// <summary>
        /// Updates the display values for the overall recipe timer and the current line timer.
        /// </summary>
        /// <param name="plcLineTime">The expected time for the current recipe line (in seconds).</param>
        /// <param name="countdownTimer">The overall recipe CountTimer instance.</param>
        /// <param name="setTotalTime">Action to update the total remaining time display (in seconds).</param>
        /// <param name="setLineTime">Action to update the current line remaining time display (in seconds).</param>
        /// <param name="logger">Logger instance for logging the update.</param>
        public static void UpdateRecipeTimeDisplay(float plcLineTime, CountTimer? countdownTimer, Action<double> setTotalTime, Action<double> setLineTime, ILogger? logger)
        {
            var recipeTimeLeft = countdownTimer?.GetRemainingTime() ?? TimeSpan.Zero;
            var lineTimeLeft = TimeSpan.FromSeconds(plcLineTime);

            setTotalTime(recipeTimeLeft.TotalSeconds);
            setLineTime(lineTimeLeft.TotalSeconds);

            logger?.LogInformation("Display updated: TotalTimeLeft = {TotalTimeLeft:F2}s, LineTimeLeft = {LineTimeLeft:F2}s",
                recipeTimeLeft.TotalSeconds, lineTimeLeft.TotalSeconds);
        }
    }
}
