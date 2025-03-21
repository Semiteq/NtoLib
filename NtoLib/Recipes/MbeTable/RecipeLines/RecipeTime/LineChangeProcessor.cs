#nullable enable
using System;

namespace NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime
{
    /// <summary>
    /// Processes changes in recipe lines and adjusts timers accordingly.
    /// Logging is active only in DEBUG builds.
    /// </summary>
    public class LineChangeProcessor : ILineChangeProcessor
    {
        private ICountTimer? _lineTimer; // Timer for the current recipe line
        private float _expectedStepTime; // Expected duration from recipe
        private readonly object _processLock = new(); // Synchronization lock

        // Named constants for thresholds.
        private const double CorrectionThreshold = 0.1; // Seconds
        private bool _lastIsRecipeActive = true;

        /// <summary>
        /// Processes the line change event and applies timer corrections.
        /// </summary>
        public void Process(bool isRecipeActive, int currentLine, float expectedStepTime, ICountTimer? countdownTimer)
        {
            lock (_processLock)
            {
                if (!isRecipeActive)
                {
                    if (_lastIsRecipeActive)
                    {
                        StopLineTimer();
                    }
                    _lastIsRecipeActive = false;
                    return;
                }

                _lastIsRecipeActive = true;

                if (_lineTimer is not null)
                {
                    ApplyTimerCorrection(countdownTimer);
                    StopLineTimer();
                }

                _expectedStepTime = expectedStepTime;
                _lineTimer = new CountTimer(TimeSpan.FromSeconds(expectedStepTime));
                _lineTimer.Start();
            }
        }

        // Applies correction based on the elapsed time of the previous line timer.
        private void ApplyTimerCorrection(ICountTimer? countdownTimer)
        {
            var actualElapsed = _lineTimer!.GetElapsedTime().TotalSeconds;
            var diffSeconds = actualElapsed - _expectedStepTime;
            if (Math.Abs(diffSeconds) > CorrectionThreshold)
            {
                if (countdownTimer is not null)
                {
                    var currentOverallRemaining = countdownTimer.GetRemainingTime();
                    var newOverallRemaining = currentOverallRemaining + TimeSpan.FromSeconds(diffSeconds);
                    countdownTimer.SetRemainingTime(newOverallRemaining);
                }
            }
        }

        // Stops and disposes the current line timer.
        private void StopLineTimer()
        {
            if (_lineTimer is null) return;
            _lineTimer.Stop();
            _lineTimer = null;
        }
    }
}
