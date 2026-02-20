using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Formulas;

public sealed class FormulaEngine
{
	private readonly IReadOnlyDictionary<short, CompiledFormula> _compiledFormulas;

	public FormulaEngine(
		IReadOnlyDictionary<short, CompiledFormula> compiledFormulas,
		ILogger<FormulaEngine> logger)
	{
		_compiledFormulas = compiledFormulas ?? throw new ArgumentNullException(nameof(compiledFormulas));
		if (logger == null)
		{
			throw new ArgumentNullException(nameof(logger));
		}

		logger.LogDebug("Received {Count} compiled formulas.", _compiledFormulas.Count);
		logger.LogTrace("Formulas are: {formulas}",
			string.Join(", ", _compiledFormulas.Select(kvp => $"[{kvp.Key}: {kvp.Value}]")));
	}

	public Result<IReadOnlyDictionary<string, double>> Calculate(
		short actionId,
		string changedVariable,
		IReadOnlyDictionary<string, double> currentValues)
	{
		var formulaResult = GetCompiledFormula(actionId);
		if (formulaResult.IsFailed)
		{
			return formulaResult.ToResult<IReadOnlyDictionary<string, double>>();
		}

		var calculationResult = formulaResult.Value.ApplyRecalculation(changedVariable, currentValues);

		return calculationResult;
	}

	private Result<CompiledFormula> GetCompiledFormula(short actionId)
	{
		return _compiledFormulas.TryGetValue(actionId, out var formula)
			? formula
			: new CoreFormulaNotFoundError(actionId);
	}
}
