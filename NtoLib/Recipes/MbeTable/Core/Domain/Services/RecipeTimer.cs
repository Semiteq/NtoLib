using System;
using System.Diagnostics;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Services
{
    public class RecipeTimer : IRecipeTimer
    {
        private Stopwatch _stopwatch;
        private TimeSpan _remaining;

        public event Action<TimeSpan> Ticked;

        public RecipeTimer()
        {
            _stopwatch = new Stopwatch();
            _remaining = TimeSpan.Zero;
        }

        public void Start(TimeSpan duration)
        {
            _remaining = duration;
            _stopwatch.Restart();
            RaiseTickEvent();
        }

        public void Pause()
        {
            if (_stopwatch.IsRunning)
            {
                _stopwatch.Stop();
                _remaining -= _stopwatch.Elapsed;
            }
        }

        public void Resume()
        {
            if (!_stopwatch.IsRunning)
            {
                _stopwatch.Restart();
            }
        }

        public void Stop()
        {
            _stopwatch.Stop();
            _remaining = TimeSpan.Zero;
        }

        public TimeSpan Remaining => _stopwatch.IsRunning ? _remaining - _stopwatch.Elapsed : _remaining;

        public bool IsRunning => _stopwatch.IsRunning;

        private void RaiseTickEvent()
        {
            Ticked?.Invoke(Remaining);
        }
    }
}
