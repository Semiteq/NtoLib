using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceModbusTCP.Errors;

public sealed class ModbusTcpTimeoutError : BilingualError
{
	public string Operation { get; }
	public int TimeoutMs { get; }

	public ModbusTcpTimeoutError(string operation, int timeoutMs)
		: base(
			$"PLC timeout during {operation} (timeout: {timeoutMs}ms)",
			$"Контроллер не отвечает при выполнении операции {operation} (тайм-аут: {timeoutMs}мс)")
	{
		Operation = operation;
		TimeoutMs = timeoutMs;
	}
}
