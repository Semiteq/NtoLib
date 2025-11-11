using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Errors;

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