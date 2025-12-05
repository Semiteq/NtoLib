using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModulePresentation.Errors;

public sealed class PresentationFileNotFoundError : BilingualError
{
	public PresentationFileNotFoundError()
		: base(
			"File not found",
			"Файл не найден")
	{
	}
}
