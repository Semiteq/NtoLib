using System.Globalization;
using System.IO;

using Serilog;
using Serilog.Core;

namespace NtoLib.OpcTreeManager.Logging;

internal static class OpcLoggerFactory
{
	private const long MaxFileSizeBytes = 5L * 1024 * 1024;
	private const int RetainedFileCount = 5;

	public static Logger Build(string logFilePath)
	{
		const string Template = "{Timestamp:O} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
		var invariant = CultureInfo.InvariantCulture;

		TryEnsureDirectory(logFilePath);

		return new LoggerConfiguration()
			.MinimumLevel.Debug()
			.WriteTo.File(
				path: logFilePath,
				rollingInterval: RollingInterval.Infinite,
				fileSizeLimitBytes: MaxFileSizeBytes,
				rollOnFileSizeLimit: true,
				retainedFileCountLimit: RetainedFileCount,
				shared: true,
				outputTemplate: Template,
				formatProvider: invariant)
			.CreateLogger();
	}

	private static void TryEnsureDirectory(string filePath)
	{
		try
		{
			var directory = Path.GetDirectoryName(filePath);

			if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
		}
		catch
		{
			// Best-effort directory creation; logging may fail if this fails
		}
	}
}
