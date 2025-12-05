namespace NtoLib.MbeTable.ModuleConfig.Dto.Columns;

public sealed class YamlColumnBusinessLogic
{
	public string PropertyTypeId { get; set; }
	public bool ReadOnly { get; set; }
	public bool SaveToCsv { get; set; }
	public YamlPlcMapping? PlcMapping { get; set; } = null;
}
