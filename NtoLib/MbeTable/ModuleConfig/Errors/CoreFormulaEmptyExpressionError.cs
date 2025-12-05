using FluentResults;

namespace NtoLib.MbeTable.ModuleConfig.Errors;

public sealed class ConfigFormulaEmptyExpressionError : Error
{
	public ConfigFormulaEmptyExpressionError()
		: base("Formula expression is empty")
	{
	}
}
