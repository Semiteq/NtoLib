using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreStepColumnNotFoundError : BilingualError
{
	public CoreStepColumnNotFoundError()
		: base(
			"Step doesn't contain property",
			"Шаг не содержит свойство")
	{
	}
}
