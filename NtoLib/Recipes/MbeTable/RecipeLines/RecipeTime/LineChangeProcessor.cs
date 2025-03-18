#nullable enable
using System;
using Microsoft.Extensions.Logging;

namespace NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime
{
    internal class LineChangeProcessor
    {
        private CountTimer? _lineTimer; // Timer for the current recipe line
        private float _previousPlcLineTime; // Expected duration from the previous cycle (in seconds)
        private int _previousLineNumber;
        private readonly object _processLock = new(); // Prevents concurrent execution
        private readonly ILogger<LineChangeProcessor> _logger;
        private readonly ILoggerFactory _loggerFactory; // Factory for creating loggers for dependent classes
        private float _previousExpectedStepTime;

        private const double CorrectionThreshold = 0.1; // Correction threshold (in seconds)
        private const double StaleTimerThreshold = 0.8; // Threshold for detecting stale timers
        private bool _lastIsRecipeActive = true; // Track previous recipe active state

        // Constructor receives logger and loggerFactory for dependency injection.
        public LineChangeProcessor(ILogger<LineChangeProcessor> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
        }


        public void Process(bool isRecipeActive, int currentLine, float expectedStepTime, CountTimer? countdownTimer)
        {
            lock (_processLock)
            {
                // If recipe is inactive, reset the timer and exit.
                if (!isRecipeActive)
                {
                    if (_lastIsRecipeActive)
                    {
                        StopTimer();
                        _logger.LogInformation("Recipe inactive: Line timer reset.");
                    }

                    _lastIsRecipeActive = false;
                    return;
                }

                _lastIsRecipeActive = true;

                // If a previous line timer exists, compute correction using its elapsed time
                // and the expected duration of the previous step.
                if (_lineTimer is not null)
                {
                    var actualElapsed = _lineTimer.GetElapsedTime().TotalSeconds;
                    var diffSeconds = actualElapsed - _previousExpectedStepTime;
                    _logger.LogInformation(
                        "Line timer: expected {Expected:F6}s, actual {Actual:F2}s, diff {Diff:F2}s",
                        _previousExpectedStepTime, actualElapsed, diffSeconds);

                    // If the difference exceeds the threshold, adjust the overall timer.
                    if (Math.Abs(diffSeconds) > CorrectionThreshold)
                    {
                        if (countdownTimer is not null)
                        {
                            var currentOverallRemaining = countdownTimer.GetRemainingTime();
                            var newOverallRemaining = currentOverallRemaining + TimeSpan.FromSeconds(diffSeconds);
                            countdownTimer.SetRemainingTime(newOverallRemaining);
                            _logger.LogInformation("Overall timer corrected from {Old:F2}s to {New:F2}s",
                                currentOverallRemaining.TotalSeconds, newOverallRemaining.TotalSeconds);
                        }
                        else
                        {
                            _logger.LogWarning("No overall timer available for correction.");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Time difference {Diff:F2}s within threshold; no correction applied.",
                            diffSeconds);
                    }

                    StopTimer();
                }
                else
                {
                    _logger.LogInformation("No previous line timer found; starting new timer.");
                }

                // Save the current step's expected duration as the new reference for the next correction.
                _previousExpectedStepTime = expectedStepTime;
                // Start a new timer using the precise expected duration (currentLineTime) for the current step.
                _lineTimer = new CountTimer(TimeSpan.FromSeconds(expectedStepTime),
                    _loggerFactory.CreateLogger<CountTimer>());
                _lineTimer.Start();
                _logger.LogInformation("New line timer started for line {Line} with duration {Duration:F6}s",
                    currentLine, expectedStepTime);
                _previousLineNumber = currentLine;
            }
        }


        /// <summary>
        /// Stops and resets the current line timer.
        /// </summary>
        private void StopTimer()
        {
            if (_lineTimer is null) return;
            _lineTimer.Stop();
            _lineTimer = null;
        }
    }
}