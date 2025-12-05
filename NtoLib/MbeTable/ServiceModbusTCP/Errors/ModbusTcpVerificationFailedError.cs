using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceModbusTCP.Errors;

public sealed class ModbusTcpVerificationFailedError : BilingualError
{
	public string Details { get; }

	public ModbusTcpVerificationFailedError(string details)
		: base(
			$"PLC verification failed: {details}",
			$"Проверка данных в контроллере не пройдена: {details}")
	{
		Details = details;
	}
}
