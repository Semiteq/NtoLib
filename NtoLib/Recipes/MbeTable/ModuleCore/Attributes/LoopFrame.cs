using System;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Attributes;

public sealed class LoopFrame
{
    public int StartIndex { get; set; }
    public int Iterations { get; set; }
    public TimeSpan LoopStartTime { get; set; }
    public TimeSpan BodyStartTime { get; set; }
}