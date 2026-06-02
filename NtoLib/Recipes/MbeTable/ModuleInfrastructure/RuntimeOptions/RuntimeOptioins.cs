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
)
{
	/// <summary>
	/// Options for the design-time editor FB, which has no Modbus connection: every
	/// transport field is neutralised so only logging and float-comparison behaviour apply.
	/// </summary>
	public static RuntimeOptions EditorDefaults(float epsilon, bool logToFile, string logFilePath)
	{
		return new RuntimeOptions(
			IpAddress: IPAddress.None,
			Port: 0,
			UnitId: 0,
			TimeoutMs: 0,
			MaxRetries: 0,
			BackoffDelayMs: 0,
			MagicNumber: 0,
			VerifyDelayMs: 0,
			ControlRegister: 0,
			FloatBaseAddr: 0,
			FloatAreaSize: 0,
			IntBaseAddr: 0,
			IntAreaSize: 0,
			WordOrder: WordOrder.HighLow,
			Epsilon: epsilon,
			LogToFile: logToFile,
			LogFilePath: logFilePath);
	}
}
