using System;
using System.Globalization;
using System.IO;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;

namespace NtoLib.Recipes.MbeTable.ServiceLogger;

public sealed class LoggingBootstrapper : IDisposable
{
    private readonly Logger? _serilogLogger;
    private readonly ILoggerFactory _factory;

    public LoggingBootstrapper(LoggingOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        if (!options.Enabled)
        {
            _factory = NullLoggerFactory.Instance;
            _serilogLogger = null;
            return;
        }

        _serilogLogger = BuildSerilogLogger(options);
        _factory = new SerilogLoggerFactory(_serilogLogger, dispose: false);
    }

    public ILoggerFactory Factory => _factory;

    public ILogger<T> CreateLogger<T>() => _factory.CreateLogger<T>();

    public void Dispose()
    {
        try
        {
            if (!ReferenceEquals(_factory, NullLoggerFactory.Instance))
                _factory.Dispose();
        }
        finally
        {
            _serilogLogger?.Dispose();
        }
    }

    private static Logger BuildSerilogLogger(LoggingOptions options)
    {
        const string template = "{UtcTimestamp:O} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
        var invariant = CultureInfo.InvariantCulture;

        var config = new LoggerConfiguration()
            .MinimumLevel.Debug()
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
        catch
        {
            // Intentionally swallow exceptions; logging will continue to console/debug.
        }
    }
}