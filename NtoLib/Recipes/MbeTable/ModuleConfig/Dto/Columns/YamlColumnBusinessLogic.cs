namespace NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Columns;

public sealed class YamlColumnBusinessLogic
{
	public string PropertyTypeId { get; set; } = string.Empty;
	public bool ReadOnly { get; set; }
	public bool SaveToCsv { get; set; }
	public YamlPlcMapping? PlcMapping { get; set; } = null;
}
