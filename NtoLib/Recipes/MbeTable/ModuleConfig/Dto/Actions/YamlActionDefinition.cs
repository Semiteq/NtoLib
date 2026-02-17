using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Actions;

public sealed class YamlActionDefinition
{
	public short Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string DeployDuration { get; set; } = string.Empty;
	public List<YamlActionColumn> Columns { get; set; } = new();
	public YamlFormulaDefinition? Formula { get; set; } = null;
}
