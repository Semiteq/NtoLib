using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceModbusTCP.Errors;

public sealed class PlcPropertyNotFoundError : BilingualError
{
	public string PropertyKey { get; }

	public PlcPropertyNotFoundError(string propertyKey)
		: base(
			$"Failed to get property '{propertyKey}' from step",
			$"Не удалось получить свойство '{propertyKey}' из шага")
	{
		PropertyKey = propertyKey;
		Metadata["propertyKey"] = propertyKey;
	}
}
