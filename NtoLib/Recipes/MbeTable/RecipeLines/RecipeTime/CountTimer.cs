using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime
{
    /// <summary>
    /// High-resolution timer using Stopwatch with state management and logging.
    /// Provides start, pause, resume, stop, and remaining time adjustment features.
    /// </summary>
    internal class CountTimer : IDisposable
    {
        private TimeSpan _duration;                                               // Total timer duration.
        private Timer _timer;                                                     // Periodic system timer for checking elapsed time.
        private readonly object _lock = new();                                    // Lock for thread safety.
        private readonly Stopwatch _stopwatch = new();                            // High-resolution stopwatch.
        private readonly TimeSpan _checkInterval = TimeSpan.FromMilliseconds(50); // Time interval for periodic checks.
        public event Action OnTimerFinished;                                      // Event triggered when timer completes.
        private TimeSpan? _lastLoggedRemainingTime;
        
        // Current state of the timer.
        private TimerState State { get; set; } = TimerState.Stopped;

        private readonly ILogger<CountTimer> _logger; // Logger instance.

        /// <summary>
        /// Initializes a new instance of CountTimer with a specified duration.
        /// </summary>
        /// <param name="duration">The total time span for the timer.</param>
        /// <param name="logger">Logger instance for logging events.</param>
        public CountTimer(TimeSpan duration, ILogger<CountTimer> logger)
        {
            _duration = duration;
            _logger = logger;
        }

        public bool IsRunning => State == TimerState.Running;
        public bool IsPaused => State == TimerState.Paused;
        public bool IsStopped => State == TimerState.Stopped;

        /// <summary>
        /// Starts the timer if it is not already running.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (State == TimerState.Running)
                {
                    _logger.LogWarning("Attempted to start timer, but timer is already running.");
                    return;
                }
                _logger.LogInformation("Starting timer with duration {Duration}.", _duration);
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
                    _logger.LogWarning("Attempted to pause timer, but timer is not running.");
                    return;
                }
                _stopwatch.Stop();
                _timer?.Dispose();
                _timer = null;
                State = TimerState.Paused;
                _logger.LogInformation("Timer paused at elapsed time {Elapsed}.", _stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Resumes the timer if it was previously paused.
        /// </summary>
        public void Resume()
        {
            lock (_lock)
            {
                if (State != TimerState.Paused)
                {
                    _logger.LogWarning("Attempted to resume timer, but timer is not paused.");
                    return;
                }
                _stopwatch.Start();
                _timer = new Timer(TimerCallback, null, _checkInterval, _checkInterval);
                State = TimerState.Running;
                _logger.LogInformation("Timer resumed.");
            }
        }

        /// <summary>
        /// Stops the timer if it is running or paused.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (State == TimerState.Stopped)
                {
                    _logger.LogWarning("Attempted to stop timer, but timer is already stopped.");
                    return;
                }
                _timer?.Dispose();
                _timer = null;
                _stopwatch.Stop();
                State = TimerState.Stopped;
                _logger.LogInformation("Timer stopped.");
            }
        }

        /// <summary>
        /// Gets the elapsed time since the timer started.
        /// </summary>
        public TimeSpan GetElapsedTime()
        {
            lock (_lock)
            {
                return _stopwatch.Elapsed;
            }
        }

        /// <summary>
        /// Gets the remaining time (duration - elapsed time).
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
        /// 
        /// </summary>
        /// <param name="newTime">New remaining duration.</param>
        public void SetRemainingTime(TimeSpan newTime)
        {
            lock (_lock)
            {
                // Update the duration and restart the stopwatch.
                _duration = newTime;
                _stopwatch.Restart();
            }
        }

        /// <summary>
        /// Periodically checks if the elapsed time has exceeded the duration.
        /// If so, stops the timer and triggers the OnTimerFinished event.
        /// </summary>
        private void TimerCallback(object state)
        {
            lock (_lock)
            {
                if (_stopwatch.Elapsed >= _duration)
                {
                    _logger.LogInformation("Timer finished. Elapsed time {Elapsed} exceeds duration {Duration}.", _stopwatch.Elapsed, _duration);
                    _timer?.Dispose();
                    _timer = null;
                    State = TimerState.Stopped;
                    OnTimerFinished?.Invoke();
                }
            }
        }

        /// <summary>
        /// Releases resources used by the timer.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                _timer?.Dispose();
                _timer = null;
            }
            _stopwatch.Stop();
            _logger.LogInformation("Timer disposed.");
        }

        /// <summary>
        /// Represents the states of the timer.
        /// </summary>
        private enum TimerState
        {
            Stopped,
            Running,
            Paused
        }
    }
}
