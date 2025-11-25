using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationAnotherOperationActiveError : BilingualError
{
	public ApplicationAnotherOperationActiveError()
		: base(
			"Another operation is already active",
			"Другая операция уже выполняется")
	{
	}
}
