using System;
using System.IO;

namespace NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;

/// <summary>
/// Resolves the absolute log-file path from a (possibly empty or environment-variable
/// containing) log directory. Falls back to <c>%AppData%\NtoLibLogs</c> when the supplied
/// directory is blank. Shared by the runtime-options providers so the resolution rule
/// lives in one place.
/// </summary>
internal static class LogFilePathResolver
{
	private const string DefaultLogsFolderName = "NtoLibLogs";
	private const string LogFileName = "mbe-table.log";

	public static string Resolve(string? logDirPath)
	{
		var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		var defaultLogsDir = Path.Combine(appData, DefaultLogsFolderName);
		var dirExpanded = Environment.ExpandEnvironmentVariables(logDirPath ?? string.Empty);
		var effectiveDir = string.IsNullOrWhiteSpace(dirExpanded) ? defaultLogsDir : dirExpanded;

		return Path.Combine(effectiveDir, LogFileName);
	}
}
