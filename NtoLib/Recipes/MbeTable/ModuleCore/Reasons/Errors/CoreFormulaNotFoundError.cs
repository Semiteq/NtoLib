using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreFormulaNotFoundError : BilingualError
{
	public CoreFormulaNotFoundError(short actionId)
		: base(
			$"No compiled formula found for action ID {actionId}",
			$"Не найдена скомпилированная формула для действия с ID {actionId}")
	{
		ActionId = actionId;
	}

	public short ActionId { get; }
}
