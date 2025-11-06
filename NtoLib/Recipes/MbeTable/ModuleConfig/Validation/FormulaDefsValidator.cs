using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Actions;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Validation;

public class FormulaDefsValidator
{
    public Result Validate(YamlFormulaDefinition formula, string context)
    {
        var structureResult = ValidateFormulaStructure(formula, context);
        if (structureResult.IsFailed)
            return structureResult;

        return ValidateRecalcOrder(formula.RecalcOrder, context);
    }

    private static Result ValidateFormulaStructure(YamlFormulaDefinition formula, string context)
    {
        var notNullCheck = ValidationCheck.NotNull(formula, context, "formula");
        if (notNullCheck.IsFailed)
            return notNullCheck;

        var recalcOrderNotNull = ValidationCheck.NotNull(formula.RecalcOrder, context, "recalc_order");
        if (recalcOrderNotNull.IsFailed)
            return recalcOrderNotNull;

        return Result.Ok();
    }

    private static Result ValidateRecalcOrder(IReadOnlyList<string> recalcOrder, string context)
    {
        if (recalcOrder.Count == 0)
        {
            return Result.Fail(new ConfigError(
                "recalc_order cannot be empty.",
                section: "ActionsDefs.yaml",
                context: context));
        }

        var uniqueFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in recalcOrder)
        {
            var fieldNotEmpty = ValidationCheck.NotEmpty(field, context, "recalc_order item");
            if (fieldNotEmpty.IsFailed)
                return fieldNotEmpty;

            var fieldUnique = ValidationCheck.Unique(field, uniqueFields, context, $"recalc_order field '{field}'");
            if (fieldUnique.IsFailed)
                return fieldUnique;
        }

        return Result.Ok();
    }
}