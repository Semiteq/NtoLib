using System;
using System.Threading;

namespace NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime
{
    internal class CountTimer
    {
        private TimeSpan _remainingSeconds;
        private Timer _timer;
        private readonly object _lock = new();

        public event Action OnTimerFinished;
        public bool started;

        public CountTimer(TimeSpan remainingSeconds)
        {
            _remainingSeconds = remainingSeconds;
        }

        public void Start()
        {
            _timer = new Timer(TimerCallback, null, 1000, 1000);
            started = true;
        }

        public void Stop()
        {
            lock (_lock)
            {
                _timer?.Dispose();
                _timer = null;
                _remainingSeconds = TimeSpan.Zero;
                started = false;
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