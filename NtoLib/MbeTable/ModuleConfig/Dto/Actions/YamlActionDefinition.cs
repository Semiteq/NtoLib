using System.Collections.Generic;

namespace NtoLib.MbeTable.ModuleConfig.Dto.Actions;

public sealed class YamlActionDefinition
{
	public short Id { get; set; }
	public string Name { get; set; }
	public string DeployDuration { get; set; }
	public List<YamlActionColumn> Columns { get; set; }
	public YamlFormulaDefinition? Formula { get; set; } = null;
}
