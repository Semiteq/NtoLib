using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreNoActionsInConfigError : BilingualError
{
	public CoreNoActionsInConfigError()
		: base(
			"No actions defined in configuration",
			"В конфигурации не определены действия")
	{
	}
}
