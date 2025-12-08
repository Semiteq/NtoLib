using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleInfrastructure.Errors;

public sealed class InfrastructureTargetsNotDefinedError : BilingualError
{
	public InfrastructureTargetsNotDefinedError(string groupName)
		: base(
			$"No targets defined for group {groupName}",
			$"Не определены цели для группы {groupName}")
	{
	}
}
