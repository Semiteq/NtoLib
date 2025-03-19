using System;
using NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime;

namespace NtoLib.Recipes.MbeTable.Tests
{
    // Fake ICountTimer implementation for testing LineChangeProcessor and RecipeTimeManager
    public class FakeCountTimer : ICountTimer
    {
        public bool IsRunning { get; set; }
        public bool IsPaused { get; set; }
        public bool IsStopped { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan RemainingTime { get; set; }
        public event Action OnTimerFinished;

        public void Start() { IsRunning = true; IsStopped = false; }
        public void Pause() { IsPaused = true; }
        public void Resume() { IsPaused = false; }
        public void Stop() { IsStopped = true; IsRunning = false; }
        public TimeSpan GetElapsedTime() => ElapsedTime;
        public TimeSpan GetRemainingTime() => RemainingTime;
        public void SetRemainingTime(TimeSpan newTime) { RemainingTime = newTime; }
        public void Dispose() { }
    }
}
