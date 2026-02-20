using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Errors;

public sealed class ConfigFormulaInvalidExpressionError : Error
{
	public ConfigFormulaInvalidExpressionError(string expression)
		: base($"Failed to parse formula expression: '{expression}'")
	{
		Expression = expression;
		WithMetadata("expression", expression);
	}

	public string Expression { get; }
}
