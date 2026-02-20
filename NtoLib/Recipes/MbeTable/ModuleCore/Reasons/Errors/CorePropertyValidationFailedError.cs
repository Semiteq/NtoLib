using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CorePropertyValidationFailedError : BilingualError
{
	public CorePropertyValidationFailedError(string reason)
		: base(
			$"Validation failed: {reason}",
			$"Ошибка валидации: {reason}")
	{
		Reason = reason;
	}

	public string Reason { get; }
}
