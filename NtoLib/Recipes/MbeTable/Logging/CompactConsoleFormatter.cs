using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace NtoLib.Recipes.MbeTable.Logging
{
    /// <summary>
    /// Compact console formatter for logging output.
    /// </summary>
    public class CompactConsoleFormatter : ConsoleFormatter
    {
        private readonly IOptions<CompactConsoleFormatterOptions> _options;

        public CompactConsoleFormatter(IOptions<CompactConsoleFormatterOptions> options)
            : base("compact") => _options = options;

        public override void Write<TState>(
            in LogEntry<TState> logEntry,
            IExternalScopeProvider scopeProvider,
            TextWriter textWriter)
        {
            var logLevel = logEntry.LogLevel switch
            {
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",
                LogLevel.Debug => "DBG",
                LogLevel.Trace => "TRC",
                _ => "???"
            };

            var category = logEntry.Category.Split('.').Last();
            var timestamp = DateTime.Now.ToString(_options.Value.TimestampFormat);

            textWriter.WriteLine($"{timestamp} [{logLevel}] [{category}] {logEntry.Formatter(logEntry.State, logEntry.Exception)}");
        }
    }
}