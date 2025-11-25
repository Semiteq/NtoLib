using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Errors;

public sealed class ConfigFormulaRecalcOrderMissingError : Error
{
	public string MissingVariables { get; }

	public ConfigFormulaRecalcOrderMissingError(string missingVariables)
		: base($"Recalculation order is missing variables: {missingVariables}")
	{
		MissingVariables = missingVariables;
		WithMetadata("missingVariables", missingVariables);
	}
}
