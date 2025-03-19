#nullable enable
using System;
using Microsoft.Extensions.Logging;

namespace NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime
{
    /// <summary>
    /// Processes changes in recipe lines and adjusts timers accordingly.
    /// Logging is active only in DEBUG builds.
    /// </summary>
    internal class LineChangeProcessor : ILineChangeProcessor
    {
        private ICountTimer? _lineTimer; // Timer for the current recipe line
        private float _previousExpectedStepTime; // Expected duration from the previous cycle
        private int _previousLineNumber;
        private readonly object _processLock = new(); // Synchronization lock
        private readonly ILogger<LineChangeProcessor> _logger;
        private readonly ILoggerFactory _loggerFactory;

        // Named constants for thresholds.
        private const double CorrectionThreshold = 0.1; // Seconds
        private bool _lastIsRecipeActive = true;

        public LineChangeProcessor(ILogger<LineChangeProcessor> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

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
#if DEBUG
                        _logger.LogInformation("Recipe inactive: Line timer reset.");
#endif
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
                else
                {
#if DEBUG
                    _logger.LogInformation("No previous line timer found; starting new timer.");
#endif
                }

                _previousExpectedStepTime = expectedStepTime;
                _lineTimer = new CountTimer(TimeSpan.FromSeconds(expectedStepTime), _loggerFactory.CreateLogger<CountTimer>());
                _lineTimer.Start();
#if DEBUG
                _logger.LogInformation("New line timer started for line {Line} with duration {Duration:F6}s", currentLine, expectedStepTime);
#endif
                _previousLineNumber = currentLine;
            }
        }

        // Applies correction based on the elapsed time of the previous line timer.
        private void ApplyTimerCorrection(ICountTimer? countdownTimer)
        {
            var actualElapsed = _lineTimer!.GetElapsedTime().TotalSeconds;
            var diffSeconds = actualElapsed - _previousExpectedStepTime;
#if DEBUG
            _logger.LogInformation("Line timer: expected {Expected:F6}s, actual {Actual:F2}s, DIFF {Diff:F2}s",
                _previousExpectedStepTime, actualElapsed, diffSeconds);
#endif
            if (Math.Abs(diffSeconds) > CorrectionThreshold)
            {
                if (countdownTimer is not null)
                {
                    var currentOverallRemaining = countdownTimer.GetRemainingTime();
                    var newOverallRemaining = currentOverallRemaining + TimeSpan.FromSeconds(diffSeconds);
                    countdownTimer.SetRemainingTime(newOverallRemaining);
#if DEBUG
                    _logger.LogInformation("Overall timer CORRECTED from {Old:F2}s to {New:F2}s",
                        currentOverallRemaining.TotalSeconds, newOverallRemaining.TotalSeconds);
#endif
                }
                else
                {
#if DEBUG
                    _logger.LogWarning("No overall timer available for correction.");
#endif
                }
            }
            else
            {
#if DEBUG
                _logger.LogInformation("Time difference {Diff:F2}s within threshold; no correction applied.", diffSeconds);
#endif
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
