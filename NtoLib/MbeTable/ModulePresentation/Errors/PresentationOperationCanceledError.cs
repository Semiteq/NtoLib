using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModulePresentation.Errors;

public sealed class PresentationOperationCanceledError : BilingualError
{
	public PresentationOperationCanceledError()
		: base(
			"Operation canceled",
			"Операция отменена")
	{
	}
}
