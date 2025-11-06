namespace NtoLib.Recipes.MbeTable.ModuleConfig.Dto.PinGroups;

public sealed class YamlPinGroupDefinition
{
    public string GroupName { get; set; }
    public int PinGroupId { get; set; }
    public int FirstPinId { get; set; }
    public int PinQuantity { get; set; }
}