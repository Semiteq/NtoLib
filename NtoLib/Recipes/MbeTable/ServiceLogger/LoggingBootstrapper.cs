using System;
using System.Globalization;
using System.IO;

using Serilog;
using Serilog.Core;

namespace NtoLib.Recipes.MbeTable.ServiceLogger;

public sealed class LoggingBootstrapper : IDisposable
{
    private readonly Logger? _serilogLogger;

    public LoggingBootstrapper(LoggingOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        if (!options.Enabled)
        {
            _serilogLogger = null;
            return;
        }

        _serilogLogger = BuildSerilogLogger(options);
        Log.Logger = _serilogLogger;
    }

    public void Dispose()
    {
        _serilogLogger?.Dispose();
    }

    private static Logger BuildSerilogLogger(LoggingOptions options)
    {
        const string template = "{UtcTimestamp:O} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
        var invariant = CultureInfo.InvariantCulture;

        var config = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .Enrich.With(new UtcTimestampEnricher())
            .WriteTo.Console(outputTemplate: template, formatProvider: invariant)
            .WriteTo.Debug(outputTemplate: template, formatProvider: invariant);

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
                outputTemplate: template,
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
                Directory.CreateDirectory(directory);
        }
        catch { /* ignoring */ }
    }
}