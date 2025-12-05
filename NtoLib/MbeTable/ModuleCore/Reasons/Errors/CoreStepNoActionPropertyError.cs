using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreStepNoActionPropertyError : BilingualError
{
	public CoreStepNoActionPropertyError()
		: base(
			"Step does not have an action property",
			"Шаг не содержит свойство действия")
	{
	}
}
