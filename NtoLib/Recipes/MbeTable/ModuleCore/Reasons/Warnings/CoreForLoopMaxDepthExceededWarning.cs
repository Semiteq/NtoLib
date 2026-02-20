using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Warnings;

public sealed class CoreForLoopMaxDepthExceededWarning : BilingualWarning
{
	public CoreForLoopMaxDepthExceededWarning(int stepIndex, int maxDepth = 3)
		: base(
			$"ForLoop exceeds maximum nesting depth ({maxDepth}) at step {stepIndex}",
			$"ForLoop превышает максимальную глубину вложенности ({maxDepth}) на шаге {stepIndex + 1}")
	{
		StepIndex = stepIndex;
		MaxDepth = maxDepth;
		Metadata["stepIndex"] = stepIndex;
		Metadata["maxDepth"] = maxDepth;
	}

	public int StepIndex { get; }
	public int MaxDepth { get; }
}
