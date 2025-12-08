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
	private readonly string _context;

	public FormulaValidator(FormulaDefinition? formula, IReadOnlyList<PropertyConfig> columns, string context)
	{
		_formula = formula;
		_columns = columns ?? throw new ArgumentNullException(nameof(columns));
		_context = context;
	}

	public Result Validate()
	{
		if (_formula == null)
			return Result.Ok();

		return ValidateExpression(_formula.Expression)
			.Bind(() => ValidateRecalcOrder(_formula.RecalcOrder))
			.Bind(() => ValidateColumnsExist(_formula.RecalcOrder, _columns));
	}

	private Result ValidateExpression(string? expression)
	{
		if (string.IsNullOrWhiteSpace(expression))
			return Result.Fail(new ConfigError(
				"Formula expression is empty.",
				section: "ActionsDefs.yaml",
				context: _context));

		return Result.Ok();
	}

	private Result ValidateRecalcOrder(IReadOnlyList<string>? recalcOrder)
	{
		return ValidateRecalcOrderIsNotEmpty(recalcOrder)
			.Bind(() => ValidateRecalcOrderHasMinimumVariables(recalcOrder!))
			.Bind(() => ValidateRecalcOrderHasNoDuplicates(recalcOrder!));
	}

	private Result ValidateRecalcOrderIsNotEmpty(IReadOnlyList<string>? recalcOrder)
	{
		if (recalcOrder == null || recalcOrder.Count == 0)
			return Result.Fail(new ConfigError(
				"Recalc order is empty.",
				section: "ActionsDefs.yaml",
				context: _context));

		return Result.Ok();
	}

	private Result ValidateRecalcOrderHasMinimumVariables(IReadOnlyList<string> recalcOrder)
	{
		if (recalcOrder.Count < 2)
			return Result.Fail(new ConfigError(
				"Recalc order must contain at least 2 variables.",
				section: "ActionsDefs.yaml",
				context: _context));

		return Result.Ok();
	}

	private Result ValidateRecalcOrderHasNoDuplicates(IReadOnlyList<string> recalcOrder)
	{
		var duplicates = FindDuplicateVariables(recalcOrder);

		if (duplicates.Any())
			return Result.Fail(new ConfigError(
				$"Recalc order contains duplicates: {string.Join(", ", duplicates)}",
				section: "ActionsDefs.yaml",
				context: _context));

		return Result.Ok();
	}

	private IReadOnlyList<string> FindDuplicateVariables(IReadOnlyList<string> recalcOrder)
	{
		return recalcOrder
			.GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
			.Where(g => g.Count() > 1)
			.Select(g => g.Key)
			.ToList();
	}

	private Result ValidateColumnsExist(
		IReadOnlyList<string> recalcOrder,
		IReadOnlyList<PropertyConfig> columns)
	{
		var columnKeys = ExtractColumnKeys(columns);
		var missingColumns = FindMissingColumns(recalcOrder, columnKeys);

		if (missingColumns.Any())
			return Result.Fail(new ConfigError(
				$"Formula references missing columns: {string.Join(", ", missingColumns)}",
				section: "ActionsDefs.yaml",
				context: _context));

		return Result.Ok();
	}

	private HashSet<string> ExtractColumnKeys(IReadOnlyList<PropertyConfig> columns)
	{
		return columns
			.Select(c => c.Key)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
	}

	private IReadOnlyList<string> FindMissingColumns(
		IReadOnlyList<string> recalcOrder,
		HashSet<string> columnKeys)
	{
		return recalcOrder
			.Where(variableName => !columnKeys.Contains(variableName))
			.ToList();
	}
}
