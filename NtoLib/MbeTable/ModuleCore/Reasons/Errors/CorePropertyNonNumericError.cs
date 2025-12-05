using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CorePropertyNonNumericError : BilingualError
{
	public CorePropertyNonNumericError()
		: base(
			"Property holds a non-numeric string value",
			"Свойство содержит нечисловое строковое значение")
	{
	}
}
