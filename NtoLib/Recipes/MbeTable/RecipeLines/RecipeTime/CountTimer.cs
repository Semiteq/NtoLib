using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime
{
    /// <summary>
    /// High-resolution timer using Stopwatch with state management.
    /// Logging is enabled only in DEBUG mode.
    /// </summary>
    internal class CountTimer : ICountTimer
    {
        // Total duration for the timer.
        private TimeSpan _duration;
        // System timer for periodic checks.
        private Timer _timer;
        // Lock object for thread safety.
        private readonly object _lock = new();
        // High-resolution stopwatch.
        private readonly Stopwatch _stopwatch = new();
        // Interval for periodic timer checks.
        private readonly TimeSpan _checkInterval = TimeSpan.FromMilliseconds(50);

        // Timer state.
        private TimerState State { get; set; } = TimerState.Stopped;

        private readonly ILogger<CountTimer> _logger;

        public event Action OnTimerFinished;

        /// <summary>
        /// Initializes a new instance of CountTimer with the specified duration.
        /// </summary>
        public CountTimer(TimeSpan duration, ILogger<CountTimer> logger)
        {
            _duration = duration;
            _logger = logger;
        }

        public bool IsRunning => State == TimerState.Running;
        public bool IsPaused => State == TimerState.Paused;
        public bool IsStopped => State == TimerState.Stopped;

        /// <summary>
        /// Starts the timer if not already running.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (State == TimerState.Running)
                {
#if DEBUG
                    _logger.LogWarning("Attempted to start timer, but timer is already running.");
#endif
                    return;
                }
#if DEBUG
                _logger.LogInformation("Starting timer with duration {Duration}.", _duration);
#endif
                _stopwatch.Restart();
                _timer = new Timer(TimerCallback, null, _checkInterval, _checkInterval);
                State = TimerState.Running;
            }
        }

        /// <summary>
        /// Pauses the timer if it is currently running.
        /// </summary>
        public void Pause()
        {
            lock (_lock)
            {
                if (State != TimerState.Running)
                {
#if DEBUG
                    _logger.LogWarning("Attempted to pause timer, but timer is not running.");
#endif
                    return;
                }
                _stopwatch.Stop();
                _timer?.Dispose();
                _timer = null;
                State = TimerState.Paused;
#if DEBUG
                _logger.LogInformation("Timer paused at elapsed time {Elapsed}.", _stopwatch.Elapsed);
#endif
            }
        }

        /// <summary>
        /// Resumes the timer if it was paused.
        /// </summary>
        public void Resume()
        {
            lock (_lock)
            {
                if (State != TimerState.Paused)
                {
#if DEBUG
                    _logger.LogWarning("Attempted to resume timer, but timer is not paused.");
#endif
                    return;
                }
                _stopwatch.Start();
                _timer = new Timer(TimerCallback, null, _checkInterval, _checkInterval);
                State = TimerState.Running;
#if DEBUG
                _logger.LogInformation("Timer resumed.");
#endif
            }
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (State == TimerState.Stopped)
                {
#if DEBUG
                    _logger.LogWarning("Attempted to stop timer, but timer is already stopped.");
#endif
                    return;
                }
                _timer?.Dispose();
                _timer = null;
                _stopwatch.Stop();
                State = TimerState.Stopped;
#if DEBUG
                _logger.LogInformation("Timer stopped.");
#endif
            }
        }

        /// <summary>
        /// Returns the elapsed time since the timer started.
        /// </summary>
        public TimeSpan GetElapsedTime()
        {
            lock (_lock)
            {
                return _stopwatch.Elapsed;
            }
        }

        /// <summary>
        /// Returns the remaining time (duration minus elapsed).
        /// </summary>
        public TimeSpan GetRemainingTime()
        {
            lock (_lock)
            {
                var remaining = _duration - _stopwatch.Elapsed;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Sets a new remaining time and restarts the stopwatch.
        /// </summary>
        public void SetRemainingTime(TimeSpan newTime)
        {
            lock (_lock)
            {
                _duration = newTime;
                _stopwatch.Restart();
            }
        }

        // Timer callback for periodic checks.
        private void TimerCallback(object state)
        {
            lock (_lock)
            {
                if (_stopwatch.Elapsed >= _duration)
                {
#if DEBUG
                    _logger.LogInformation("Timer finished. Elapsed time {Elapsed} exceeds duration {Duration}.", _stopwatch.Elapsed, _duration);
#endif
                    _timer?.Dispose();
                    _timer = null;
                    State = TimerState.Stopped;
                    OnTimerFinished?.Invoke();
                }
            }
        }

        /// <summary>
        /// Disposes the timer and releases resources.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                _timer?.Dispose();
                _timer = null;
            }
            _stopwatch.Stop();
#if DEBUG
            _logger.LogInformation("Timer disposed.");
#endif
        }

        // Internal timer state enumeration.
        private enum TimerState
        {
            Stopped,
            Running,
            Paused
        }
    }
}
