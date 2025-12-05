using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceModbusTCP.Errors;

public sealed class ModbusTcpSerializationError : BilingualError
{
	public string PropertyKey { get; }

	public ModbusTcpSerializationError(string propertyKey)
		: base(
			$"Failed to serialize property '{propertyKey}' for PLC",
			$"Не удалось сериализовать свойство '{propertyKey}' для контроллера")
	{
		PropertyKey = propertyKey;
	}
}
