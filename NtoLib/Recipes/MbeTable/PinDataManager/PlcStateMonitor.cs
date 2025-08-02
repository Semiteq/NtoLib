using System;

namespace NtoLib.Recipes.MbeTable.PinDataManager;

public class PlcStateMonitor : IPlcStateMonitor
{
    public event Action<PlcLineInfo> CurrentLineChanged;

    public float StepCurrentTime { get; private set; }
    public int CurrentLineNumber { get; private set; }

    private PlcLineInfo _lastLineInfo = new(-1, -1, -1, -1);

    public void UpdateState(int lineNumber, int loop1, int loop2, int loop3, float currentTime)
    {
        var newLineInfo = new PlcLineInfo(lineNumber, loop1, loop2, loop3);
        StepCurrentTime = currentTime;
        CurrentLineNumber = lineNumber;
        if (!newLineInfo.Equals(_lastLineInfo))
        {
            _lastLineInfo = newLineInfo;
            CurrentLineChanged?.Invoke(_lastLineInfo);
        }
    }
}