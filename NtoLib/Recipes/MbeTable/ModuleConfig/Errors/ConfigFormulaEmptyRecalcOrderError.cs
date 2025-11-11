using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Errors;

public sealed class ConfigFormulaEmptyRecalcOrderError : Error
{
    public ConfigFormulaEmptyRecalcOrderError()
        : base("Recalculation order is empty")
    {
    }
}