using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationUnexpectedIoWriteError : BilingualError
{
	public ApplicationUnexpectedIoWriteError()
		: base(
			"Unexpected error during write operation",
			"Непредвиденная ошибка при записи")
	{
	}
}
