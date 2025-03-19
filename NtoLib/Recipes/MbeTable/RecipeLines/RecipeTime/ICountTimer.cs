using System;

namespace NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime
{
    // Interface for high-resolution timer to allow unit testing and decoupling.
    public interface ICountTimer : IDisposable
    {
        bool IsRunning { get; }
        bool IsPaused { get; }
        bool IsStopped { get; }
        void Start();
        void Pause();
        void Resume();
        void Stop();
        TimeSpan GetElapsedTime();
        TimeSpan GetRemainingTime();
        void SetRemainingTime(TimeSpan newTime);
        event Action OnTimerFinished;
    }
}