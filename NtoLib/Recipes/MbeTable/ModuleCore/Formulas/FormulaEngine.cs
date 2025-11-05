using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Formulas;

public sealed class FormulaEngine : IFormulaEngine
{
    private readonly IReadOnlyDictionary<short, CompiledFormula> _compiledFormulas;
    private readonly ILogger<FormulaEngine> _logger;

    public FormulaEngine(IActionRepository actionRepository, ILogger<FormulaEngine> logger)
    {
        _ = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _compiledFormulas = CompileFormulasAtStartup(actionRepository.Actions);
    }

    private IReadOnlyDictionary<short, CompiledFormula> CompileFormulasAtStartup(IReadOnlyDictionary<short, ActionDefinition> actions)
    {
        var actionsWithFormulas = actions.Values.Where(a => a.Formula != null).ToList();
        var compiledFormulas = new Dictionary<short, CompiledFormula>(actionsWithFormulas.Count);

        foreach (var action in actionsWithFormulas)
        {
            var compilationResult = CompileActionFormula(action);
            if (compilationResult.IsFailed)
                ThrowCriticalCompilationError(action, compilationResult.Errors);

            compiledFormulas[action.Id] = compilationResult.Value;
            LogSuccessfulCompilation(action);
        }

        return compiledFormulas;
    }
    
    private Result<CompiledFormula> CompileActionFormula(ActionDefinition action)
    {
        return CompiledFormula.Create(
            action.Formula!.Expression,
            action.Formula.RecalcOrder,
            _logger);
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

    private void ThrowCriticalCompilationError(ActionDefinition action, IReadOnlyList<IError> errors)
    {
        var errorMessage = string.Join("; ", errors.Select(e => e.Message));
        
        _logger.LogCritical(
            "Failed to compile formula for action {ActionId} '{ActionName}': {Expression}. Errors: {Errors}",
            action.Id, action.Name, action.Formula?.Expression ?? "null", errorMessage);

        throw new InvalidOperationException(
            $"Failed to compile formula for action {action.Id} '{action.Name}': {errorMessage}");
    }
    
    private void LogSuccessfulCompilation(ActionDefinition action)
    {
        _logger.LogInformation(
            "Successfully compiled formula for action {ActionId} '{ActionName}': {Expression}",
            action.Id, action.Name, action.Formula!.Expression);
    }
}