using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CorePropertyValidationFailedError : BilingualError
{
	public string Reason { get; }

	public CorePropertyValidationFailedError(string reason)
		: base(
			$"Validation failed: {reason}",
			$"Ошибка валидации: {reason}")
	{
		Reason = reason;
	}
}
