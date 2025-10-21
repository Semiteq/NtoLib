
namespace NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;

/// <summary>
/// DTO for YAML deserialization of property definitions.
/// </summary>
public sealed class YamlPropertyDefinition
{
    public string PropertyTypeId { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty;
    public string Units { get; set; } = string.Empty;

    // Numeric boundaries. May be unused for non-numeric types.
    public float? Min { get; set; }
    public float? Max { get; set; }

    // String-specific
    public int? MaxLength { get; set; }

    // Formatting strategy: Auto, Scientific, TimeHms.
    public string FormatKind { get; set; } = "Numeric";
}