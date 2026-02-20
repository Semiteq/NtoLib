using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreStepMissingActionError : BilingualError
{
	public CoreStepMissingActionError(int stepIndex)
		: base(
			$"Step at index {stepIndex} is missing Action property",
			$"Шаг с индексом {stepIndex + 1} не содержит свойство Action")
	{
		StepIndex = stepIndex;
	}

	public int StepIndex { get; }
}
