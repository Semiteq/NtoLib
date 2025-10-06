

namespace NtoLib.Recipes.MbeTable.Config.Domain.Actions;

/// <summary>
/// Defines a single column that is applicable for a specific action,
/// including its semantic type id, optional default value, and an optional
/// pin-group data source when PropertyTypeId is "Enum".
/// </summary>
public sealed class PropertyConfig
{
    /// <summary>
    /// The column key (must match a Key in ColumnDefs.yaml).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Defines the semantic property type id (e.g., "Enum", "Temp", "Time", "Percent").
    /// </summary>
    public string PropertyTypeId { get; set; } = string.Empty;

    /// <summary>
    /// Optional default value to initialize new steps (YAML scalar). StepBuilder will convert it to SystemType from registry.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Optional pin group name that provides enum options for this column.
    /// Required when PropertyTypeId == "Enum" and the values are sourced from pin groups.
    /// </summary>
    public string? GroupName { get; set; }
}