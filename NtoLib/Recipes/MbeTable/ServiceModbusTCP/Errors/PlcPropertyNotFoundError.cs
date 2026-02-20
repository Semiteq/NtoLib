using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Errors;

public sealed class PlcPropertyNotFoundError : BilingualError
{
	public PlcPropertyNotFoundError(string propertyKey)
		: base(
			$"Failed to get property '{propertyKey}' from step",
			$"Не удалось получить свойство '{propertyKey}' из шага")
	{
		PropertyKey = propertyKey;
		Metadata["propertyKey"] = propertyKey;
	}

	public string PropertyKey { get; }
}
