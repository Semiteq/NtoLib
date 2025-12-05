using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceModbusTCP.Errors;

public sealed class ModbusTcpUnexpectedError : BilingualError
{
	public ModbusTcpUnexpectedError(string msg)
		: base(
			$"Unexpected error occurred during serialization of property: {msg}",
			$"Во время сериализации свойства произошла непредвиденная ошибка: {msg}")
	{
	}
}
