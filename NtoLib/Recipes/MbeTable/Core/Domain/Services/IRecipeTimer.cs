using System;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Services
{
    public interface IRecipeTimer
    {
        event Action<TimeSpan> Ticked;
        void Start(TimeSpan duration);
        void Pause();
        void Resume();
        void Stop();
        TimeSpan Remaining { get; }
        bool IsRunning { get; }
    }
}