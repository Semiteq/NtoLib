using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Errors;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Domain;

public sealed class RecipeComparator
{
	private readonly IRuntimeOptionsProvider _optionsProvider;

	public RecipeComparator(IRuntimeOptionsProvider optionsProvider)
	{
		_optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
	}

	public Result Compare(Recipe recipe1, Recipe recipe2)
	{
		if (recipe1 is null)
			throw new ArgumentNullException(nameof(recipe1));
		if (recipe2 is null)
			throw new ArgumentNullException(nameof(recipe2));

		if (recipe1.Steps.Count != recipe2.Steps.Count)
			return Result.Fail(new ModbusTcpVerificationFailedError(
				$"Row count differs: {recipe1.Steps.Count} vs {recipe2.Steps.Count}"));

		for (var i = 0; i < recipe1.Steps.Count; i++)
		{
			var cmp = CompareSteps(recipe1.Steps[i], recipe2.Steps[i]);
			if (cmp.IsFailed)
				return Result.Fail(new ModbusTcpVerificationFailedError(
					$"Row {i} differs: {string.Join("; ", cmp.Errors.Select(e => e.Message))}"));
		}

		return Result.Ok();
	}

	public Result CompareSteps(Step a, Step b)
	{
		var epsilon = _optionsProvider.GetCurrent().Epsilon;

		var excluded = new HashSet<ColumnIdentifier>
		{
			MandatoryColumns.StepStartTime,
			MandatoryColumns.Comment
		};

		var keys = a.Properties.Keys
			.Union(b.Properties.Keys)
			.Where(k => !excluded.Contains(k))
			.ToArray();

		foreach (var key in keys)
		{
			a.Properties.TryGetValue(key, out var pa);
			b.Properties.TryGetValue(key, out var pb);

			var va = pa?.GetValueAsObject;
			var vb = pb?.GetValueAsObject;

			if (!ValueEquals(va, vb, epsilon))
				return Result.Fail(new ModbusTcpVerificationFailedError(
					$"Key={key.Value}, A='{Format(va)}', B='{Format(vb)}'"));
		}

		return Result.Ok();
	}

	private string Format(object? v) => v switch
	{
		null => "null",
		float f => f.ToString("R", CultureInfo.InvariantCulture),
		double d => d.ToString("R", CultureInfo.InvariantCulture),
		_ => v.ToString() ?? "null"
	};

	private static bool ValueEquals(object? a, object? b, double epsilon)
	{
		if (a is null && b is null)
			return true;
		if (a is null || b is null)
			return false;

		if (TryToDouble(a, out var da) && TryToDouble(b, out var db))
			return Math.Abs(da - db) <= epsilon;

		if (a is string sa && b is string sb)
			return string.Equals(sa.Trim(), sb.Trim(), StringComparison.Ordinal);

		return Equals(a, b);
	}

	private static bool TryToDouble(object v, out double d)
	{
		switch (v)
		{
			case float f:
				d = f;
				return true;
			case double dd:
				d = dd;
				return true;
			case int i:
				d = i;
				return true;
			case long l:
				d = l;
				return true;
			case string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed):
				d = parsed;
				return true;
			default:
				d = 0;
				return false;
		}
	}
}
