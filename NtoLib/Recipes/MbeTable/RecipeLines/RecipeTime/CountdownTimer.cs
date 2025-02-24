using System;
using System.Threading;

namespace NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime
{
    internal class CountdownTimer
    {
        private TimeSpan _remainingSeconds;
        private Timer _timer;
        private readonly object _lock = new();

        public event Action OnTimerFinished;

        public CountdownTimer(TimeSpan remainingSeconds)
        {
            _remainingSeconds = remainingSeconds;
        }

        public void Start()
        {
            _timer = new Timer(TimerCallback, null, 1000, 1000);
        }

        public void Pause()
        {
            lock (_lock)
            {
                _timer?.Dispose();
                _timer = null;
            }
        }

        public void Resume()
        {
            lock (_lock)
            {
                _timer ??= new Timer(TimerCallback, null, 1000, 1000);
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _timer?.Dispose();
                _timer = null;
                _remainingSeconds = TimeSpan.Zero;
            }
        }

        public TimeSpan GetRemainingTime()
        {
            lock (_lock)
            {
                return _remainingSeconds;
            }
        }

        public void SetRemainingTime(TimeSpan newTime)
        {
            lock (_lock)
            {
                _remainingSeconds = newTime;
            }
        }


        private void TimerCallback(object state)
        {
            lock (_lock)
            {
                if (_remainingSeconds > TimeSpan.Zero)
                {
                    _remainingSeconds = _remainingSeconds.Subtract(TimeSpan.FromSeconds(1));

                    if (_remainingSeconds <= TimeSpan.Zero)
                    {
                        _timer?.Dispose();
                        OnTimerFinished?.Invoke();
                    }
                }
            }
        }
    }
}