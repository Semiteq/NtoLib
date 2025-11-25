using System;
using System.Globalization;
using System.IO;

using Serilog;
using Serilog.Core;

namespace NtoLib.Recipes.MbeTable.ServiceLogger;

public sealed class LoggingBootstrapper : IDisposable
{
	private Logger? _serilogLogger;
	private readonly LoggingOptions _loggingOptions;

	public LoggingBootstrapper(LoggingOptions options)
	{
		_loggingOptions = options ?? throw new ArgumentNullException(nameof(options));
	}

	public void Initialize()
	{
		if (!_loggingOptions.Enabled)
		{
			return;
		}

		_serilogLogger = BuildSerilogLogger(_loggingOptions);
		Log.Logger = _serilogLogger;
	}

	public void Dispose()
	{
		_serilogLogger?.Dispose();
		Log.CloseAndFlush();
	}

	private static Logger BuildSerilogLogger(LoggingOptions options)
	{
		const string Template = "{Timestamp:O} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
		var invariant = CultureInfo.InvariantCulture;

		var config = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.Enrich.FromLogContext()
			.WriteTo.Console(outputTemplate: Template, formatProvider: invariant)
			.WriteTo.Debug(outputTemplate: Template, formatProvider: invariant);

		if (!string.IsNullOrWhiteSpace(options.FilePath))
		{
			TryEnsureDirectory(options.FilePath);

			config = config.WriteTo.File(
				path: options.FilePath,
				rollingInterval: RollingInterval.Infinite,
				fileSizeLimitBytes: 5 * 1024 * 1024,
				rollOnFileSizeLimit: true,
				retainedFileCountLimit: 5,
				shared: true,
				outputTemplate: Template,
				formatProvider: invariant);
		}

		return config.CreateLogger();
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
			// Ignoring
		}
	}
}
