namespace NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;

public sealed class YamlPropertyDefinition
{
	public string PropertyTypeId { get; set; }
	public string SystemType { get; set; }
	public string Units { get; set; }
	public bool NonNegative { get; set; }
	public float? Min { get; set; } = null;
	public float? Max { get; set; } = null;
	public int? MaxLength { get; set; } = null;
	public string FormatKind { get; set; }
}
