using NtoLib.MbeTable.ModuleCore.Entities;

namespace NtoLib.MbeTable.ModuleCore.Analyzer;

public interface ITimingCalculator
{
	TimingResult Calculate(Recipe recipe, LoopSemanticsResult loopSemantics);
}
