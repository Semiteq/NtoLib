using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Formulas;

public sealed class FormulaEngine : IFormulaEngine
{
    private readonly IReadOnlyDictionary<short, CompiledFormula> _compiledFormulas;
    private readonly ILogger<FormulaEngine> _logger;

    public FormulaEngine(
        IReadOnlyDictionary<short, CompiledFormula> compiledFormulas,
        ILogger<FormulaEngine> logger)
    {
        _compiledFormulas = compiledFormulas ?? new Dictionary<short, CompiledFormula>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogDebug("Received {Count} compiled formulas.", _compiledFormulas.Count);
        _logger.LogTrace("Formulas are: {formulas}", string.Join(", ", _compiledFormulas.Select(kvp => $"[{kvp.Key}: {kvp.Value}]")));
    }

    public Result<IReadOnlyDictionary<string, double>> Calculate(
        short actionId,
        string changedVariable,
        IReadOnlyDictionary<string, double> currentValues)
    {
        var formulaResult = GetCompiledFormula(actionId);
        if (formulaResult.IsFailed)
            return formulaResult.ToResult<IReadOnlyDictionary<string, double>>();

        var calculationResult = formulaResult.Value.ApplyRecalculation(changedVariable, currentValues);
        return calculationResult;
    }

    private Result<CompiledFormula> GetCompiledFormula(short actionId)
    {
        return _compiledFormulas.TryGetValue(actionId, out var formula)
            ? formula
            : Errors.FormulaNotFound(actionId);
    }
}