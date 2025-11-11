using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Errors;

public sealed class ConfigFormulaEmptyExpressionError : Error
{
    public ConfigFormulaEmptyExpressionError()
        : base("Formula expression is empty")
    {
    }
}