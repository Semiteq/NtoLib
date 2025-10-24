using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Attributes;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;

namespace NtoLib.Recipes.MbeTable.ModuleCore;

/// <summary>
/// Core domain service managing Recipe state and coordinating mutations and analysis.
/// Owns the current Recipe and provides pure business logic operations.
/// </summary>
public sealed class RecipeService : IRecipeService
{
    private Recipe _currentRecipe;

    private readonly RecipeMutator _mutator;
    private readonly IRecipeAttributesService _attributesService;

    public event Action<bool>? ValidationStateChanged
    {
        add => _attributesService.ValidationStateChanged += value;
        remove => _attributesService.ValidationStateChanged -= value;
    }
    
    /// <exception cref="InvalidOperationException">If recipe attributes update fails</exception>
    public RecipeService(
        RecipeMutator mutator,
        IRecipeAttributesService attributesService)
    {
        _mutator = mutator ?? throw new ArgumentNullException(nameof(mutator));
        _attributesService = attributesService ?? throw new ArgumentNullException(nameof(attributesService));
        _currentRecipe = Recipe.Empty;

        var analysisResult = _attributesService.UpdateAttributes(_currentRecipe);
        if (analysisResult.IsFailed)
        {
            throw new InvalidOperationException(
                $"Failed to initialize RecipeService with provided recipe: {analysisResult.Errors[0].Message}");
        }
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

    public Result SetRecipe(Recipe recipe)
    {
        var analysisResult = _attributesService.UpdateAttributes(recipe);
        if (analysisResult.IsFailed)
            return analysisResult;

        _currentRecipe = recipe;
        return Result.Ok();
    }

    public Result AddStep(int rowIndex)
    {
        var mutationResult = _mutator.AddDefaultStep(_currentRecipe, rowIndex);
        if (mutationResult.IsFailed)
            return mutationResult.ToResult();

        return SetRecipe(mutationResult.Value);
    }

    public Result RemoveStep(int rowIndex)
    {
        var mutationResult = _mutator.RemoveStep(_currentRecipe, rowIndex);
        if (mutationResult.IsFailed)
            return mutationResult.ToResult();

        return SetRecipe(mutationResult.Value);
    }

    public Result UpdateStepProperty(int rowIndex, ColumnIdentifier key, object value)
    {
        var mutationResult = _mutator.UpdateStepProperty(_currentRecipe, rowIndex, key, value);
        if (mutationResult.IsFailed)
            return mutationResult.ToResult();

        var analysisResult = _attributesService.UpdateAttributes(mutationResult.Value);
        if (analysisResult.IsFailed)
            return analysisResult;

        _currentRecipe = mutationResult.Value;
        return Result.Ok();
    }

    public Result ReplaceStepAction(int rowIndex, short newActionId)
    {
        var mutationResult = _mutator.ReplaceStepAction(_currentRecipe, rowIndex, newActionId);
        if (mutationResult.IsFailed)
            return mutationResult.ToResult();

        return SetRecipe(mutationResult.Value);
    }
}