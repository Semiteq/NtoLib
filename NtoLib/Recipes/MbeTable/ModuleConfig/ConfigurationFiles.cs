

using System.IO;

namespace NtoLib.Recipes.MbeTable.ModuleConfig;

/// <summary>
/// Defines the paths to configuration files required for application initialization.
/// </summary>
public sealed record ConfigurationFiles
{
    public string PropertyDefsPath { get; }
    public string ColumnDefsPath { get; }
    public string PinGroupDefsPath { get; }
    public string ActionDefsPath { get; }

    public ConfigurationFiles(
        string baseDirectory,
        string propertyDefsFileName,
        string columnDefsFileName,
        string pinGroupDefsFileName,
        string actionsDefsFileName)
    {
        PropertyDefsPath = Path.Combine(baseDirectory, propertyDefsFileName);
        ColumnDefsPath = Path.Combine(baseDirectory, columnDefsFileName);
        PinGroupDefsPath = Path.Combine(baseDirectory, pinGroupDefsFileName);
        ActionDefsPath = Path.Combine(baseDirectory, actionsDefsFileName);
    }
}