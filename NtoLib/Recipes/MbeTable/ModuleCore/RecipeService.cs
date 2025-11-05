using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Attributes;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Formulas;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore;

public sealed class RecipeService : IRecipeService
{
    private Recipe _currentRecipe = Recipe.Empty;

    private readonly RecipeMutator _mutator;
    private readonly IRecipeAttributesService _attributesService;
    private readonly FormulaApplicationCoordinator _formulaCoordinator;
    private readonly IActionRepository _actionRepository;
    private readonly ILogger<RecipeService> _logger;

    public event Action<bool>? ValidationStateChanged
    {
        add => _attributesService.ValidationStateChanged += value;
        remove => _attributesService.ValidationStateChanged -= value;
    }

    public RecipeService(
        RecipeMutator mutator,
        IRecipeAttributesService attributesService,
        FormulaApplicationCoordinator formulaCoordinator,
        IActionRepository actionRepository,
        ILogger<RecipeService> logger)
    {
        _mutator = mutator ?? throw new ArgumentNullException(nameof(mutator));
        _attributesService = attributesService ?? throw new ArgumentNullException(nameof(attributesService));
        _formulaCoordinator = formulaCoordinator ?? throw new ArgumentNullException(nameof(formulaCoordinator));
        _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Recipe GetCurrentRecipe() => _currentRecipe;

    public Result<TimeSpan> GetStepStartTime(int stepIndex) =>
        _attributesService.GetStepStartTime(stepIndex);

    public TimeSpan GetTotalDuration() =>
        _attributesService.GetTotalDuration();

    public IReadOnlyDictionary<int, TimeSpan> GetAllStepStartTimes() =>
        _attributesService.GetAllStepStartTimes();

    public bool IsValid() =>
        _attributesService.IsValid();

    public Result<int> GetLoopNestingLevel(int stepIndex) =>
        _attributesService.GetLoopNestingLevel(stepIndex);

    public IReadOnlyList<LoopMetadata> GetEnclosingLoops(int stepIndex) =>
        _attributesService.GetEnclosingLoops(stepIndex);
    
    public Result SetRecipeAndUpdateAttributes(Recipe recipe)
    {
        var analysisResult = _attributesService.UpdateAttributes(recipe);
        if (analysisResult.IsFailed)
            return analysisResult;

        _currentRecipe = recipe;
        return analysisResult;
    }

    public Result AddStep(int rowIndex)
    {
        var mutationResult = _mutator.AddDefaultStep(_currentRecipe, rowIndex);

        return mutationResult.IsSuccess
            ? SetRecipeAndUpdateAttributes(mutationResult.Value)
            : mutationResult.ToResult();
    }

    public Result RemoveStep(int rowIndex)
    {
        var mutationResult = _mutator.RemoveStep(_currentRecipe, rowIndex);
        
        return mutationResult.IsSuccess 
            ? SetRecipeAndUpdateAttributes(mutationResult.Value)
            : mutationResult.ToResult();
    }

    public Result UpdateStepProperty(int rowIndex, ColumnIdentifier columnIdentifier, object value)
    {
        if (rowIndex < 0 || rowIndex >= _currentRecipe.Steps.Count)
            return Result.Fail(Errors.IndexOutOfRange(rowIndex, _currentRecipe.Steps.Count));

        var actionResult = GetActionForStep(rowIndex);
        if (actionResult.IsFailed)
        {
            LogOperationFailure("GetActionForStep", actionResult.ToResult(), new { RowIndex = rowIndex });
            return actionResult.ToResult();
        }

        var mutationResult = _mutator.UpdateStepProperty(_currentRecipe, rowIndex, columnIdentifier, value);
        if (mutationResult.IsFailed)
        {
            LogOperationFailure("UpdateStepProperty", mutationResult.ToResult(),
                new { RowIndex = rowIndex, Column = columnIdentifier.Value, Value = value });
            return mutationResult.ToResult();
        }

        var mutatedRecipe = mutationResult.Value;
        var step = mutatedRecipe.Steps[rowIndex];

        var formulaResult = _formulaCoordinator.ApplyIfExists(step, actionResult.Value, columnIdentifier);
        if (formulaResult.IsFailed)
        {
            LogOperationFailure("ApplyFormula", formulaResult.ToResult(),
                new { RowIndex = rowIndex, Column = columnIdentifier.Value });
            return formulaResult.ToResult();
        }

        var stepsWithFormula = mutatedRecipe.Steps.SetItem(rowIndex, formulaResult.Value);
        var recipeWithFormulas = new Recipe(stepsWithFormula);

        var analysisResult = _attributesService.UpdateAttributes(recipeWithFormulas);
        if (analysisResult.IsFailed)
        {
            LogOperationFailure("UpdateAttributes", analysisResult, new { RowIndex = rowIndex });
            return analysisResult;
        }

        _currentRecipe = recipeWithFormulas;
        return analysisResult;
    }

    public Result ReplaceStepAction(int rowIndex, short newActionId)
    {
        var mutationResult = _mutator.ReplaceStepAction(_currentRecipe, rowIndex, newActionId);
        if (mutationResult.IsFailed)
        {
            LogOperationFailure("ReplaceStepAction", mutationResult.ToResult(),
                new { RowIndex = rowIndex, NewActionId = newActionId });
            return mutationResult.ToResult();
        }

        var mutatedRecipe = mutationResult.Value;

        var analysisResult = _attributesService.UpdateAttributes(mutatedRecipe);
        if (analysisResult.IsFailed)
        {
            LogOperationFailure("UpdateAttributes", analysisResult, new { RowIndex = rowIndex });
            return analysisResult;
        }

        _currentRecipe = mutatedRecipe;
        return analysisResult;
    }

    private Result<ActionDefinition> GetActionForStep(int rowIndex)
    {
        var step = _currentRecipe.Steps[rowIndex];
        var actionProperty = step.Properties[MandatoryColumns.Action];
        if (actionProperty == null)
            return Result.Fail(Errors.StepActionPropertyNull(rowIndex));

        var valueResult = actionProperty.GetValue<short>();
        if (valueResult.IsFailed)
            return valueResult.ToResult();

        var actionId = valueResult.Value;
        var actionResult = _actionRepository.GetActionDefinitionById(actionId);
        if (actionResult.IsFailed)
            return actionResult.ToResult();

        return actionResult;
    }

    private void LogOperationFailure(string operation, Result result, object? context = null)
    {
        var errorChainEn = string.Join(" → ", result.Errors
            .OfType<BilingualError>()
            .Select(e => e.MessageEn)
            .DefaultIfEmpty(result.Errors.FirstOrDefault()?.Message ?? "Unknown error"));

        if (context != null)
        {
            _logger.LogWarning("{Operation} failed: {ErrorChain}. Context: {@Context}",
                operation, errorChainEn, context);
        }
        else
        {
            _logger.LogWarning("{Operation} failed: {ErrorChain}", operation, errorChainEn);
        }
    }
}