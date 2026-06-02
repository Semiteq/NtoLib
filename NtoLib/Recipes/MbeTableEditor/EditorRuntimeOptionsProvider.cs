using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;

namespace NtoLib.Recipes.MbeTableEditor;

public sealed class EditorRuntimeOptionsProvider : IRuntimeOptionsProvider
{
	private readonly float _epsilon;
	private readonly string _logDirPath;
	private readonly bool _logToFile;

	public EditorRuntimeOptionsProvider(bool logToFile, string logDirPath, float epsilon)
	{
		_logToFile = logToFile;
		_logDirPath = logDirPath ?? string.Empty;
		_epsilon = epsilon;
	}

	public RuntimeOptions GetCurrent()
	{
		var logFilePath = LogFilePathResolver.Resolve(_logDirPath);

		return RuntimeOptions.EditorDefaults(_epsilon, _logToFile, logFilePath);
	}
}
