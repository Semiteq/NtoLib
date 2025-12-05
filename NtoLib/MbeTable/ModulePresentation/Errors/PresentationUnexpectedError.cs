using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModulePresentation.Errors;

public sealed class PresentationUnexpectedError : BilingualError
{
	public string Details { get; }

	public PresentationUnexpectedError(string details)
		: base(
			$"Unexpected error: {details}",
			$"Непредвиденная ошибка: {details}")
	{
		Details = details;
		Metadata["details"] = details;
	}
}
