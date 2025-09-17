using System;

namespace NtoLib.Recipes.MbeTable.Config;

public record ConfigFiles
{
    public string BaseDirectory { get; }
    public string ColumnDefsFileName { get; }
    public string ActionsDefsFileName { get; }
    public string PinGroupDefsFileName { get; }
    public string PropertyDefsFileName { get; }

    /// <summary>
    /// Initializes a new instance of the ConfigFiles record.
    /// Throws ArgumentException if any argument is null or empty.
    /// </summary>
    public ConfigFiles(
        string baseDirectory,
        string[] configFiles)
    {
        BaseDirectory = baseDirectory;
        ColumnDefsFileName = configFiles[0];
        ActionsDefsFileName = configFiles[1];
        PinGroupDefsFileName = configFiles[2];
        PropertyDefsFileName = configFiles[3];
    }
}