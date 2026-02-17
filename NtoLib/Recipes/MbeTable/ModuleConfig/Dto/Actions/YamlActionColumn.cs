namespace NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Actions;

public sealed class YamlActionColumn
{
	public string Key { get; set; } = string.Empty;
	public string PropertyTypeId { get; set; } = string.Empty;
	public string? GroupName { get; set; } = null;
	public string? DefaultValue { get; set; } = null;
}
