using System;

namespace NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime
{
    internal sealed class FlattenedRecipeLine
    {
        public int OriginalIndex { get; }
        public TimeSpan AbsoluteExecutionTime { get; }
        public int Depth1 { get; }
        public int Depth2 { get; }
        public int Depth3 { get; }
        public TimeSpan ExecutionTime { get; }

        public FlattenedRecipeLine(int originalIndex, TimeSpan absoluteExecutionTime, int depth1, int depth2, int depth3, TimeSpan executionTime)
        {
            OriginalIndex = originalIndex;
            AbsoluteExecutionTime = absoluteExecutionTime;
            Depth1 = depth1;
            Depth2 = depth2;
            Depth3 = depth3;
            ExecutionTime = executionTime;
        }
    }
}