namespace NtoLib.Recipes.MbeTable.Logging;

public sealed class LoggingOptions
{
    public bool Enabled { get; }
    public string FilePath { get; }

    public LoggingOptions(bool enabled, string filePath)
    {
        Enabled = enabled;
        FilePath = filePath ?? string.Empty;
    }
}