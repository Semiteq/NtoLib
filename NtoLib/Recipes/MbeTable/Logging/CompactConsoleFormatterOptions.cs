using Microsoft.Extensions.Logging.Console;

namespace NtoLib.Recipes.MbeTable.Logging
{
    /// <summary>
    /// Options for the compact console formatter.
    /// </summary>
    public class CompactConsoleFormatterOptions : ConsoleFormatterOptions
    {
        public string TimestampFormat { get; set; } = "HH:mm:ss";
    }
}