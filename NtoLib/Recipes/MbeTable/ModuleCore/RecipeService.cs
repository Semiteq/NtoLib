using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Attributes;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Errors;
using NtoLib.Recipes.MbeTable.ModuleCore.Formulas;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModuleCore.Warnings;
using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore;

public sealed class RecipeService : IRecipeService
{
    public Recipe CurrentRecipe { get; private set; } = Recipe.Empty;

    private readonly RecipeMutator _mutator;
    private readonly IRecipeAttributesService _attributesService;
    private readonly FormulaApplicationCoordinator _formulaCoordinator;
    private readonly IActionRepository _actionRepository;
    private readonly ILogger<RecipeService> _logger;

    public int StepCount => CurrentRecipe.Steps.Count;

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

    public Result<ValidationSnapshot> SetRecipeAndUpdateAttributes(Recipe recipe)
    {
        var analysisResult = _attributesService.UpdateAttributes(recipe);
        if (analysisResult.IsFailed)
            return analysisResult.ToResult<ValidationSnapshot>();

        CurrentRecipe = recipe;

        var snapshot = _attributesService.GetValidationSnapshot();
        var ok = Result.Ok(snapshot);

        if (analysisResult.Reasons.Any())
            ok.WithReasons(analysisResult.Reasons);

        return ok;
    }

    public Result<ValidationSnapshot> AddStep(int rowIndex)
    {
        var mutationResult = _mutator.AddDefaultStep(CurrentRecipe, rowIndex);
        if (mutationResult.IsFailed)
            return mutationResult.ToResult<ValidationSnapshot>();

        return SetRecipeAndUpdateAttributes(mutationResult.Value);
    }

    public Result<ValidationSnapshot> RemoveStep(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= CurrentRecipe.Steps.Count)
            return new CoreIndexOutOfRangeError(rowIndex, CurrentRecipe.Steps.Count);

        var mutationResult = _mutator.RemoveStep(CurrentRecipe, rowIndex);
        if (mutationResult.IsFailed)
            return mutationResult.ToResult<ValidationSnapshot>();

        var setResult = SetRecipeAndUpdateAttributes(mutationResult.Value);

        if (setResult.IsSuccess && mutationResult.Value.Steps.Count == 0)
            return setResult.WithReason(new CoreEmptyRecipeWarning());

        return setResult;
    }

    public Result<ValidationSnapshot> UpdateStepProperty(int rowIndex, ColumnIdentifier columnIdentifier, object value)
    {
        if (rowIndex < 0 || rowIndex >= CurrentRecipe.Steps.Count)
            return new CoreIndexOutOfRangeError(rowIndex, CurrentRecipe.Steps.Count);

        var actionResult = GetActionForStep(rowIndex);
        if (actionResult.IsFailed)
        {
            LogOperationFailure("GetActionForStep", actionResult.ToResult(), new { RowIndex = rowIndex });
            return actionResult.ToResult<ValidationSnapshot>();
        }

        var mutationResult = _mutator.UpdateStepProperty(CurrentRecipe, rowIndex, columnIdentifier, value);
        if (mutationResult.IsFailed)
        {
            LogOperationFailure("UpdateStepProperty", mutationResult.ToResult(),
                new { RowIndex = rowIndex, Column = columnIdentifier.Value, Value = value });
            return mutationResult.ToResult<ValidationSnapshot>();
        }

        var mutatedRecipe = mutationResult.Value;
        var step = mutatedRecipe.Steps[rowIndex];

        var formulaResult = _formulaCoordinator.ApplyIfExists(step, actionResult.Value, columnIdentifier);
        if (formulaResult.IsFailed)
        {
            LogOperationFailure("ApplyFormula", formulaResult.ToResult(),
                new { RowIndex = rowIndex, Column = columnIdentifier.Value });

            return formulaResult
                .WithError(new CoreStepPropertyUpdateFailedError(rowIndex, columnIdentifier.Value))
                .ToResult<ValidationSnapshot>();
        }

        var stepsWithFormula = mutatedRecipe.Steps.SetItem(rowIndex, formulaResult.Value);
        var recipeWithFormulas = new Recipe(stepsWithFormula);

        return AnalyzeAndCommit(recipeWithFormulas, rowIndex);
    }

    public Result<ValidationSnapshot> ReplaceStepAction(int rowIndex, short newActionId)
    {
        var mutationResult = _mutator.ReplaceStepAction(CurrentRecipe, rowIndex, newActionId);
        if (mutationResult.IsFailed)
        {
            LogOperationFailure("ReplaceStepAction", mutationResult.ToResult(),
                new { RowIndex = rowIndex, NewActionId = newActionId });
            return mutationResult.ToResult<ValidationSnapshot>();
        }

        return AnalyzeAndCommit(mutationResult.Value, rowIndex);
    }

    private Result<ActionDefinition> GetActionForStep(int rowIndex)
    {
        var step = CurrentRecipe.Steps[rowIndex];

        if (!step.Properties.TryGetValue(MandatoryColumns.Action, out var actionProperty) || actionProperty == null)
            return new CoreStepActionPropertyNullError(rowIndex);

        var valueResult = actionProperty.GetValue<short>();
        if (valueResult.IsFailed)
            return valueResult.ToResult();

        var actionId = valueResult.Value;
        var actionResult = _actionRepository.GetActionDefinitionById(actionId);
        if (actionResult.IsFailed)
            return actionResult.ToResult();

        return actionResult;
    }

    private Result<ValidationSnapshot> AnalyzeAndCommit(Recipe recipe, int rowIndexForLog)
    {
        var analysisResult = _attributesService.UpdateAttributes(recipe);
        if (analysisResult.IsFailed)
        {
            LogOperationFailure("UpdateAttributes", analysisResult, new { RowIndex = rowIndexForLog });
            return analysisResult.ToResult<ValidationSnapshot>();
        }

        CurrentRecipe = recipe;

        var snapshot = _attributesService.GetValidationSnapshot();
        var ok = Result.Ok(snapshot);

        if (analysisResult.Reasons.Any())
            ok.WithReasons(analysisResult.Reasons);

        return ok;
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