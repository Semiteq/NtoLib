using FluentResults;

namespace NtoLib.MbeTable.ModuleConfig.Errors;

public sealed class ConfigFormulaInvalidExpressionError : Error
{
	public string Expression { get; }

	public ConfigFormulaInvalidExpressionError(string expression)
		: base($"Failed to parse formula expression: '{expression}'")
	{
		Expression = expression;
		WithMetadata("expression", expression);
	}
}
