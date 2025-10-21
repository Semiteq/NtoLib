namespace NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Actions;

/// <summary>
/// DTO for deserializing a single column reference inside an action from YAML.
/// </summary>
public sealed class YamlActionColumn
{
    /// <summary>
    /// Gets or sets the key of the referenced column.
    /// </summary>
    public string Key { get; set; } = null!;

    /// <summary>
    /// Gets or sets the property type ID for this column in the action (may override column definition).
    /// </summary>
    public string PropertyTypeId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the group name for enum columns, must refer to a pin group if PropertyTypeId is 'Enum'.
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// Gets or sets the default value, if any (optional).
    /// </summary>
    public string? DefaultValue { get; set; }
}