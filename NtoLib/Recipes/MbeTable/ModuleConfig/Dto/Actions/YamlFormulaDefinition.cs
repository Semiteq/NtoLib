using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Actions;

public sealed class YamlFormulaDefinition
{
	public string Expression { get; set; }
	public List<string> RecalcOrder { get; set; }
}
