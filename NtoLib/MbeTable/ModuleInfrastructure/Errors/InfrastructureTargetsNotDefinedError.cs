using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleInfrastructure.Errors;

public sealed class InfrastructureTargetsNotDefinedError : BilingualError
{
	public InfrastructureTargetsNotDefinedError(string groupName)
		: base(
			$"No targets defined for group {groupName}",
			$"Не определены цели для группы {groupName}")
	{
	}
}
