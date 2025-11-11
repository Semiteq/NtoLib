using System;

using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Errors;

public sealed class ModbusTcpConnectionFailedError : BilingualError
{
    public string IpAddress { get; }
    public int Port { get; }
    public Exception? InnerException { get; }

    public ModbusTcpConnectionFailedError(string ipAddress, int port, Exception? innerException = null)
        : base(
            $"Failed to connect to PLC at {ipAddress}:{port}",
            $"Не удалось подключиться к контроллеру {ipAddress}:{port}")
    {
        IpAddress = ipAddress;
        Port = port;
        InnerException = innerException;
        
        if (innerException != null)
            CausedBy(innerException);
    }
}