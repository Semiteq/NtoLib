using System.Net;

namespace NtoLib.Recipes.MbeTable.ModbusTCP.Configuration;

public enum WordOrder
{
    HighLow,
    LowHigh
}

/// <summary>
/// Immutable TCP / Modbus settings provided by runtime configuration.
/// </summary>
public sealed record ModbusOptions(
    IPAddress IpAddress,
    int Port,
    byte UnitId,
    int TimeoutMs,
    int MaxRetries,
    int MagicNumber,
    int VerifyDelayMs,
    
    int ControlRegister,
    int FloatBaseAddr,
    int FloatAreaSize,
    int IntBaseAddr,
    int IntAreaSize,
    
    WordOrder WordOrder,
    float Epsilon);