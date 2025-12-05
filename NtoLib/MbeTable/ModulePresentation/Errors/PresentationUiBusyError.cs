using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModulePresentation.Errors;

public sealed class PresentationUiBusyError : BilingualError
{
	public PresentationUiBusyError()
		: base(
			"UI is busy",
			"UI занят")
	{
	}
}
