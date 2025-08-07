namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

public class PlcLineInfo
{
    public readonly int LineNumber;
    public readonly int LoopCount1;
    public readonly int LoopCount2;
    public readonly int LoopCount3;
    
    public PlcLineInfo(int lineNumber, int loopCount1, int loopCount2, int loopCount3)
    {
        LineNumber = lineNumber;
        LoopCount1 = loopCount1;
        LoopCount2 = loopCount2;
        LoopCount3 = loopCount3;
    }
}