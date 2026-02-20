using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreActionNotFoundError : BilingualError
{
	public CoreActionNotFoundError(short actionId)
		: base(
			$"Action with ID {actionId} not found",
			$"Действие с ID {actionId} не найдено")
	{
		ActionId = actionId;
	}

	public short ActionId { get; }
}
