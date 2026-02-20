using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class AssemblyStepFailedError : BilingualError
{
	public AssemblyStepFailedError(int stepIndex)
		: base(
			$"Failed to assemble step {stepIndex}",
			$"Не удалось собрать шаг {stepIndex}")
	{
		StepIndex = stepIndex;
		Metadata["stepIndex"] = stepIndex;
	}

	public int StepIndex { get; }
}
