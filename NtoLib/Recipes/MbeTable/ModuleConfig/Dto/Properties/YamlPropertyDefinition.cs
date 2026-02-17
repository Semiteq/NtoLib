namespace NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;

public sealed class YamlPropertyDefinition
{
	public string PropertyTypeId { get; set; } = string.Empty;
	public string SystemType { get; set; } = string.Empty;
	public string Units { get; set; } = string.Empty;
	public bool NonNegative { get; set; }
	public float? Min { get; set; } = null;
	public float? Max { get; set; } = null;
	public int? MaxLength { get; set; } = null;
	public string FormatKind { get; set; } = string.Empty;
}
