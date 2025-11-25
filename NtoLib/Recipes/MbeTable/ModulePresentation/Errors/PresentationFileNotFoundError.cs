using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Errors;

public sealed class PresentationFileNotFoundError : BilingualError
{
	public PresentationFileNotFoundError()
		: base(
			"File not found",
			"Файл не найден")
	{
	}
}
