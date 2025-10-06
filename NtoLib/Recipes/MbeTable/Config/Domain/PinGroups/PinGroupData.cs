
namespace NtoLib.Recipes.MbeTable.Config.Domain.PinGroups;

/// <summary>
/// DTO for JSON deserialization of PinGroupDefs.yaml.
/// Must have a parameterless constructor and settable properties for System.Text.Json on .NET Framework 4.8.
/// </summary>
public record PinGroupData(string GroupName, int PinGroupId, int FirstPinId, int PinQuantity);