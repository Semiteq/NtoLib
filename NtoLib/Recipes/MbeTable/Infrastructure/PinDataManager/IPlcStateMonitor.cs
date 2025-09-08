using System;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

public interface IPlcStateMonitor
{
    event Action<PlcLineInfo> CurrentLineChanged;

    float StepCurrentTime { get; }
    int CurrentLineNumber { get; }

    void UpdateState(int lineNumber, int loop1, int loop2, int loop3, float currentTime);
}