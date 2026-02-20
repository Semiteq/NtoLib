using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Errors;

public sealed class ConfigFormulaRecalcOrderMissingError : Error
{
	public ConfigFormulaRecalcOrderMissingError(string missingVariables)
		: base($"Recalculation order is missing variables: {missingVariables}")
	{
		MissingVariables = missingVariables;
		WithMetadata("missingVariables", missingVariables);
	}

	public string MissingVariables { get; }
}
