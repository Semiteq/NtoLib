namespace NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Columns;

public sealed class YamlColumnDefinition
{
	public string Key { get; set; } = string.Empty;
	public YamlColumnBusinessLogic BusinessLogic { get; set; } = null!;
	public YamlColumnUi Ui { get; set; } = null!;
}
