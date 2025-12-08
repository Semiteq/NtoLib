using System.Net;

namespace NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;

public enum WordOrder
{
	HighLow,
	LowHigh
}

public sealed record RuntimeOptions(
	IPAddress IpAddress,
	int Port,
	byte UnitId,
	int TimeoutMs,
	int MaxRetries,
	int BackoffDelayMs,
	int MagicNumber,
	int VerifyDelayMs,
	int ControlRegister,
	int FloatBaseAddr,
	int FloatAreaSize,
	int IntBaseAddr,
	int IntAreaSize,
	WordOrder WordOrder,
	float Epsilon,
	bool LogToFile,
	string LogFilePath
);
