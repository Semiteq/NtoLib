#nullable enable
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.Actions.TableLines;

namespace NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime
{
    // Implementation of IRecipeTimeManager
    public class RecipeTimeManager : IRecipeTimeManager
    {
        // Recipe data and UI control reference.
        private List<RecipeLine>? _tableData;
        private DataGridView? _dataGridView;
        private List<FlattenedRecipeLine>? _flattenedData;

        // Overall timer tracking.
        private DateTime _overallTimerStart;
        private bool _isRecipeRunning;
        public TimeSpan TotalTime { get; private set; }

        // Named constant for correction threshold (in seconds)
        private const double CorrectionThreshold = 0.1;

        /// <summary>
        /// Sets the recipe data and initializes flattened data.
        /// </summary>
        public void SetData(List<RecipeLine>? tableData, DataGridView? dataGridView)
        {
            _tableData = tableData;
            _dataGridView = dataGridView;
            _flattenedData = FlattenRecipe();
        }

        /// <summary>
        /// Recalculates the execution time for each recipe line and updates the UI.
        /// </summary>
        public void Recalculate()
        {
            if (_tableData is null || _tableData.Count == 0)
            {
                TotalTime = TimeSpan.Zero;
                return;
            }

            var accumulatedTime = TimeSpan.Zero;
            // Initialize first row time
            _tableData[0].Time = 0f;
            UpdateDataGridView(0, TimeSpan.Zero);

            for (var rowIndex = 0; rowIndex < _tableData.Count; rowIndex++)
            {
                var recipeLine = _tableData[rowIndex];
                var lineTime = GetLineTime(recipeLine, rowIndex);
                accumulatedTime += lineTime;

                if (rowIndex < _tableData.Count - 1)
                {
                    _tableData[rowIndex + 1].Time = (float)accumulatedTime.TotalSeconds;
                    UpdateDataGridView(rowIndex + 1, accumulatedTime);
                }
            }

            TotalTime = accumulatedTime;
        }

        // Extracted method to calculate time for a single recipe line.
        private TimeSpan GetLineTime(RecipeLine recipeLine, int rowIndex)
        {
            return recipeLine switch
            {
                EndFor_Loop endLoop => TimeSpan.FromSeconds(CalculateCycleTime(endLoop, rowIndex)),
                _ when recipeLine.ActionTime == ActionTime.TimeSetpoint => TimeSpan.FromSeconds(recipeLine.Duration),
                _ => TimeSpan.Zero
            };
        }

        /// <summary>
        /// Retrieves the execution time for a specific recipe line based on its original index and loop depths.
        /// </summary>
        public TimeSpan GetRowTime(int originalIndex, int depth1, int depth2, int depth3)
        {
            if (_flattenedData is null) return TimeSpan.Zero;

            var row = _flattenedData.Find(r =>
                r.OriginalIndex == originalIndex &&
                r.Depth1 == depth1 &&
                r.Depth2 == depth2 &&
                r.Depth3 == depth3);
            return row?.ExecutionTime ?? TimeSpan.Zero;
        }

        // Calculates the cycle time for an EndFor_Loop line.
        private float CalculateCycleTime(EndFor_Loop endLoop, int rowIndex)
        {
            var cycleStartIndex = FindCycleStart(rowIndex);
            return cycleStartIndex == -1
                ? 0f
                : (endLoop.Time - _tableData![cycleStartIndex].Time) * (_tableData[cycleStartIndex].Setpoint - 1);
        }

        // Updates the DataGridView for a given row.
        private void UpdateDataGridView(int rowIndex, TimeSpan time)
        {
            if (_dataGridView is null)
                return;
            _dataGridView.Rows[rowIndex].Cells[Params.RecipeTimeIndex].Value = time.ToString("hh\\:mm\\:ss\\.ff");
        }

        // Finds the starting index of a For_Loop for a given EndFor_Loop.
        private int FindCycleStart(int endIndex)
        {
            var tabulateLevel = 1;
            for (var i = endIndex - 1; i >= 0; i--)
            {
                if (_tableData![i] is EndFor_Loop)
                    tabulateLevel++;
                else if (_tableData[i] is For_Loop)
                    tabulateLevel--;

                if (tabulateLevel == 0)
                    return i;
            }

            return -1;
        }

        // Flattens the nested recipe structure into a linear list for easier processing.
        private List<FlattenedRecipeLine>? FlattenRecipe()
        {
            var flattened = new List<FlattenedRecipeLine>();
            var accumulatedTime = TimeSpan.Zero;
            var loopStack = new Stack<(int startIndex, int iterations, int currentIteration)>();

            for (var i = 0; i < _tableData!.Count; i++)
            {
                var line = _tableData[i];
                var maxIterations = 1;

                if (line is For_Loop forLoop)
                {
                    maxIterations = (int)line.Setpoint;
                    loopStack.Push((i, maxIterations, 1));
                }
                else if (line is EndFor_Loop && loopStack.Count > 0)
                {
                    maxIterations = loopStack.Peek().iterations;
                }

                // Determine current depths from loop stack
                var depth1 = loopStack.Count > 0 ? loopStack.Peek().currentIteration : 0;
                var depth2 = loopStack.Count > 1 ? loopStack.ToArray()[1].currentIteration : 0;
                var depth3 = loopStack.Count > 2 ? loopStack.ToArray()[2].currentIteration : 0;

                var execTime = line.ActionTime == ActionTime.TimeSetpoint
                    ? TimeSpan.FromSeconds(line.Duration)
                    : TimeSpan.Zero;
                flattened.Add(new FlattenedRecipeLine(i, accumulatedTime, depth1, depth2, depth3, execTime));

                if (line is EndFor_Loop && loopStack.Count > 0)
                {
                    var (startIdx, iterations, currentIteration) = loopStack.Pop();
                    if (currentIteration < iterations)
                    {
                        loopStack.Push((startIdx, iterations, currentIteration + 1));
                        i = startIdx; // Restart loop from beginning of For_Loop block
                    }
                }
            }

            return flattened;
        }

        /// <summary>
        /// Manages the overall recipe timer based on recipe activity.
        /// For unit testing, verify state transitions and timer corrections.
        /// </summary>
        public ICountTimer? ManageRecipeTimer(bool isRecipeActive, ICountTimer? currentTimer, TimeSpan totalRecipeTime)
        {

            if (!isRecipeActive)
            {
                if (_isRecipeRunning)
                {
                    currentTimer?.Stop();
                    _isRecipeRunning = false;
                }

                return null;
            }

            if (totalRecipeTime == TimeSpan.Zero)
                return currentTimer;

            if (!_isRecipeRunning)
            {
                _overallTimerStart = DateTime.UtcNow;
                currentTimer = new CountTimer(totalRecipeTime);
                currentTimer.Start();
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
        /// Updates the UI display for overall recipe time and current line time.
        /// Designed for unit testing of display update logic.
        /// </summary>
        public void UpdateRecipeTimeDisplay(float plcLineTime, ICountTimer? countdownTimer, Action<double> setTotalTime,
            Action<double> setLineTime)
        {
            var recipeTimeLeft = countdownTimer?.GetRemainingTime() ?? TimeSpan.Zero;
            var lineTimeLeft = TimeSpan.FromSeconds(plcLineTime);

            setTotalTime(recipeTimeLeft.TotalSeconds);
            setLineTime(lineTimeLeft.TotalSeconds);
        }
    }
}