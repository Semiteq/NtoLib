#nullable enable
namespace NtoLib.Recipes.MbeTable.Config.Models.ActionTargets;

/// <summary>
/// DTO for JSON deserialization of PinGroups.json.
/// Must have a parameterless constructor and settable properties for System.Text.Json on .NET Framework 4.8.
/// </summary>
public class PinGroupData
{
    /// <summary>
    /// The unique string identifier for this group (e.g., "Shutters", "Heaters").
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Explicit group ID to use when creating the FB group node.
    /// </summary>
    public int PinGroupId { get; set; }

    /// <summary>
    /// The numeric ID of the first pin in this group.
    /// </summary>
    public int FirstPinId { get; set; }

    /// <summary>
    /// The total number of pins in this group.
    /// </summary>
    public int PinQuantity { get; set; }

    /// <summary>
    /// Parameterless constructor required by System.Text.Json on .NET Framework 4.8.
    /// </summary>
    public PinGroupData()
    {
    }

    /// <summary>
    /// Convenience constructor for manual creation.
    /// </summary>
    public PinGroupData(string groupName, int pinGroupId, int firstPinId, int pinQuantity)
    {
        GroupName = groupName;
        PinGroupId = pinGroupId;
        FirstPinId = firstPinId;
        PinQuantity = pinQuantity;
    }
}