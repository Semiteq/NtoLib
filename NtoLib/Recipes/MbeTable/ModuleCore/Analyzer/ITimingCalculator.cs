using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Analyzer;

public interface ITimingCalculator
{
    TimingResult Calculate(Recipe recipe, LoopSemanticsResult loopSemantics);
}