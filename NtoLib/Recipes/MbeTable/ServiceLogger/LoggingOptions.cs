using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;

namespace NtoLib.Recipes.MbeTable.ServiceLogger;

public sealed class LoggingOptions
{
	public bool Enabled { get; }
	public string FilePath { get; }

	public LoggingOptions(IRuntimeOptionsProvider runtimeOptionsProvider)
	{
		var runtimeOptions = runtimeOptionsProvider.GetCurrent();
		Enabled = runtimeOptions.LogToFile;
		FilePath = runtimeOptions.LogFilePath ?? string.Empty;
	}
}
