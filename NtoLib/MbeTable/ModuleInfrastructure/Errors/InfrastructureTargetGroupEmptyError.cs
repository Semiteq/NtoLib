using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleInfrastructure.Errors;

public sealed class InfrastructureTargetGroupEmptyError : BilingualError
{
	public InfrastructureTargetGroupEmptyError(string? groupName)
		: base(
			$"Target group {groupName} contains zero targets",
			$"Группа пинов {groupName} содержит ноль пинов")
	{
	}
}
