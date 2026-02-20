using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreStepNullError : BilingualError
{
	public CoreStepNullError(int stepIndex)
		: base(
			$"Step at index {stepIndex} is null",
			$"Шаг с индексом {stepIndex + 1} равен null")
	{
		StepIndex = stepIndex;
	}

	public int StepIndex { get; }
}
