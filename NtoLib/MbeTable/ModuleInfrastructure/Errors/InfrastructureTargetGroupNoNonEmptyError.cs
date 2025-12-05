using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleInfrastructure.Errors;

public sealed class InfrastructureTargetGroupNoNonEmptyError : BilingualError
{
	public InfrastructureTargetGroupNoNonEmptyError(string? groupName)
		: base(
			$"Target group {groupName} contains zero non zero targets",
			$"Группа пинов {groupName} не содержит ни одного непустого пина")
	{
	}
}
