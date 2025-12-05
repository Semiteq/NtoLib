using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreActionNameEmptyError : BilingualError
{
	public CoreActionNameEmptyError()
		: base(
			"Action name is empty",
			"Имя действия пустое")
	{
	}
}
