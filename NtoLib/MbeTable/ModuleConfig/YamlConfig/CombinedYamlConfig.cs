namespace NtoLib.MbeTable.ModuleConfig.YamlConfig;

/// <summary>
/// Aggregate container holding all validated configuration sections.
/// Replaces the monolithic RawConfiguration.
/// </summary>
public sealed record CombinedYamlConfig(
	PropertyDefsYamlConfig PropertyDefs,
	ColumnDefsYamlConfig ColumnDefs,
	PinGroupDefsYamlConfig PinGroupDefs,
	ActionDefsYamlConfig ActionDefs);
