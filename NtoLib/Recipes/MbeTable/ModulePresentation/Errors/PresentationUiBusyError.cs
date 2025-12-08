using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Errors;

public sealed class PresentationUiBusyError : BilingualError
{
	public PresentationUiBusyError()
		: base(
			"UI is busy",
			"UI занят")
	{
	}
}
