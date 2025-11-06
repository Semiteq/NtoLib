using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Validation;

public sealed class FormulaValidator
{
    private readonly FormulaDefinition? _formula;
    private readonly IReadOnlyList<PropertyConfig> _columns;

    public FormulaValidator(FormulaDefinition? formula, IReadOnlyList<PropertyConfig> columns)
    {
        _formula = formula;
        _columns = columns ?? throw new ArgumentNullException(nameof(columns));
    }

    public Result Validate()
    {
        if (_formula == null)
            return Result.Ok();

        return ValidateExpression(_formula.Expression)
            .Bind(() => ValidateRecalcOrder(_formula.RecalcOrder))
            .Bind(() => ValidateColumnsExist(_formula.RecalcOrder, _columns));
    }

    private static Result ValidateExpression(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return Result.Fail(new ConfigError(
                "Formula expression is empty.",
                section: "ActionsDefs.yaml",
                context: "formula-validation"));

        return Result.Ok();
    }

    private static Result ValidateRecalcOrder(IReadOnlyList<string>? recalcOrder)
    {
        if (recalcOrder == null || recalcOrder.Count == 0)
            return Result.Fail(new ConfigError(
                "Recalc order is empty.",
                section: "ActionsDefs.yaml",
                context: "formula-validation"));

        if (recalcOrder.Count < 2)
            return Result.Fail(new ConfigError(
                "Recalc order must contain at least 2 variables.",
                section: "ActionsDefs.yaml",
                context: "formula-validation"));

        var duplicates = recalcOrder
            .GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
            return Result.Fail(new ConfigError(
                $"Recalc order contains duplicates: {string.Join(", ", duplicates)}",
                section: "ActionsDefs.yaml",
                context: "formula-validation"));

        return Result.Ok();
    }

    private static Result ValidateColumnsExist(
        IReadOnlyList<string> recalcOrder,
        IReadOnlyList<PropertyConfig> columns)
    {
        var columnKeys = columns
            .Select(c => c.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingColumns = recalcOrder
            .Where(varName => !columnKeys.Contains(varName))
            .ToList();

        if (missingColumns.Any())
            return Result.Fail(new ConfigError(
                $"Formula references missing columns: {string.Join(", ", missingColumns)}",
                section: "ActionsDefs.yaml",
                context: "formula-validation"));

        return Result.Ok();
    }
}