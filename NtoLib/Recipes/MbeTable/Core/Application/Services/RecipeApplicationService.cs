#nullable enable

using System;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Core.Application.Services;

/// <summary>
/// Orchestrates recipe modification and analysis operations.
/// </summary>
public sealed class RecipeApplicationService : IRecipeApplicationService
{
    private readonly IRecipeEngine _recipeEngine;
    private readonly IRecipeLoopValidator _recipeLoopValidator;
    private readonly IRecipeTimeCalculator _recipeTimeCalculator;

    public RecipeApplicationService(
        IRecipeEngine recipeEngine,
        IRecipeLoopValidator recipeLoopValidator,
        IRecipeTimeCalculator recipeTimeCalculator)
    {
        _recipeEngine = recipeEngine ?? throw new ArgumentNullException(nameof(recipeEngine));
        _recipeLoopValidator = recipeLoopValidator ?? throw new ArgumentNullException(nameof(recipeLoopValidator));
        _recipeTimeCalculator = recipeTimeCalculator ?? throw new ArgumentNullException(nameof(recipeTimeCalculator));
    }

    public RecipeUpdateResult CreateEmpty()
    {
        var emptyRecipe = _recipeEngine.CreateEmptyRecipe();
        return AnalyzeRecipe(emptyRecipe);
    }

    public RecipeUpdateResult AddDefaultStep(Recipe currentRecipe, int rowIndex)
    {
        var newRecipe = _recipeEngine.AddDefaultStep(currentRecipe, rowIndex);
        return AnalyzeRecipe(newRecipe);
    }

    public RecipeUpdateResult RemoveStep(Recipe currentRecipe, int rowIndex)
    {
        var newRecipe = _recipeEngine.RemoveStep(currentRecipe, rowIndex);
        return AnalyzeRecipe(newRecipe);
    }

    public RecipeUpdateResult ReplaceStepWithNewDefault(Recipe currentRecipe, int rowIndex, int newActionId)
    {
        var newRecipe = _recipeEngine.ReplaceStepWithNewDefault(currentRecipe, rowIndex, newActionId);
        return AnalyzeRecipe(newRecipe);
    }

    public Result<RecipeUpdateResult> UpdateStepProperty(Recipe currentRecipe, int rowIndex, ColumnIdentifier key, object value)
    {
        var updateResult = _recipeEngine.UpdateStepProperty(currentRecipe, rowIndex, key, value);

        if (updateResult.IsFailed)
        {
            return updateResult.ToResult<RecipeUpdateResult>();
        }

        var analysisResult = AnalyzeRecipe(updateResult.Value);
        return Result.Ok(analysisResult);
    }

    /// <inheritdoc />
    public RecipeUpdateResult AnalyzeRecipe(Recipe recipe)
    {
        var loopResult = _recipeLoopValidator.Validate(recipe);
        var timeResult = _recipeTimeCalculator.Calculate(recipe);
        return new RecipeUpdateResult(recipe, loopResult, timeResult);
    }
}