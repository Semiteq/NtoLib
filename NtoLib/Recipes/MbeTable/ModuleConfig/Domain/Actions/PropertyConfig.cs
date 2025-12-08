namespace NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;

public sealed class PropertyConfig
{
	public string Key { get; set; } = string.Empty;
	public string PropertyTypeId { get; set; } = string.Empty;
	public string? DefaultValue { get; set; }
	public string? GroupName { get; set; }
}
