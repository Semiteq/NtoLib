using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class AssemblyStepFailedError : BilingualError
{
	public int StepIndex { get; }

	public AssemblyStepFailedError(int stepIndex)
		: base(
			$"Failed to assemble step {stepIndex}",
			$"Не удалось собрать шаг {stepIndex}")
	{
		StepIndex = stepIndex;
		Metadata["stepIndex"] = stepIndex;
	}
}
