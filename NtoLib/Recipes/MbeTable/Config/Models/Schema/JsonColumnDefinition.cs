#nullable enable

namespace NtoLib.Recipes.MbeTable.Config.Models.Schema;

/// <summary>
/// A temporary class used for deserializing the JSON schema configuration.
/// Must be a class with a parameterless constructor for System.Text.Json compatibility.
/// </summary>
public class JsonColumnDefinition
{
    // Initialize collections to prevent null reference issues after deserialization.
    public string Key { get; set; } = string.Empty;
    public int Index { get; set; }
    public string Code { get; set; } = string.Empty;
    public string UiName { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty;
    public int Width { get; set; }
    public bool ReadOnly { get; set; }
    public string Alignment { get; set; } = string.Empty;
    public PlcMapping? PlcMapping { get; set; }
}