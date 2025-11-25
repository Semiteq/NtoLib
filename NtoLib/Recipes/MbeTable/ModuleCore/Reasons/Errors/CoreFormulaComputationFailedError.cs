using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreFormulaComputationFailedError : BilingualError
{
	public string Details { get; }

	public CoreFormulaComputationFailedError(string details)
		: base(
			$"Formula computation failed: {details}",
			$"Не удалось вычислить формулу: {details}")
	{
		Details = details;
	}
}
