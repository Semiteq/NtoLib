namespace NtoLib.Recipes.MbeTable.ModuleConfig.Dto.PinGroups;

/// <summary>
/// DTO for deserializing a single pin group definition from YAML.
/// </summary>
public sealed class YamlPinGroupDefinition
{
    /// <summary>
    /// Gets or sets the unique group name, used for reference in ActionsDefs.yaml.
    /// </summary>
    public string GroupName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique pin group ID in the hardware configuration.
    /// </summary>
    public int PinGroupId { get; set; }

    /// <summary>
    /// Gets or sets the first pin ID in the group.
    /// </summary>
    public int FirstPinId { get; set; }

    /// <summary>
    /// Gets or sets the quantity of pins in this group.
    /// </summary>
    public int PinQuantity { get; set; }
}