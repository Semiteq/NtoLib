using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Errors;

public sealed class ConfigFormulaRecalcOrderExtraError : Error
{
	public ConfigFormulaRecalcOrderExtraError(string extraVariables)
		: base($"Recalculation order contains variables not present in formula: {extraVariables}")
	{
		ExtraVariables = extraVariables;
		WithMetadata("extraVariables", extraVariables);
	}

	public string ExtraVariables { get; }
}
