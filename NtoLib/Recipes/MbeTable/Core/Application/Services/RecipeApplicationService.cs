#nullable enable

using System;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Core.Application.Services
{
    public record RecipeUpdateResult(Recipe Recipe, LoopValidationResult LoopResult, RecipeTimeAnalysis TimeResult);

    public interface IRecipeApplicationService
    {
        RecipeUpdateResult CreateEmpty();
        RecipeUpdateResult AddDefaultStep(Recipe currentRecipe, int rowIndex);
        RecipeUpdateResult RemoveStep(Recipe currentRecipe, int rowIndex);
        Result<RecipeUpdateResult> UpdateStepProperty(Recipe currentRecipe, int rowIndex, ColumnIdentifier key, object value);
        RecipeUpdateResult ReplaceStepWithNewDefault(Recipe currentRecipe, int rowIndex, int newActionId);
    }

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
            return Analyze(emptyRecipe);
        }

        public RecipeUpdateResult AddDefaultStep(Recipe currentRecipe, int rowIndex)
        {
            var newRecipe = _recipeEngine.AddDefaultStep(currentRecipe, rowIndex);
            return Analyze(newRecipe);
        }

        public RecipeUpdateResult RemoveStep(Recipe currentRecipe, int rowIndex)
        {
            var newRecipe = _recipeEngine.RemoveStep(currentRecipe, rowIndex);
            return Analyze(newRecipe);
        }

        public RecipeUpdateResult ReplaceStepWithNewDefault(Recipe currentRecipe, int rowIndex, int newActionId)
        {
            var newRecipe = _recipeEngine.ReplaceStepWithNewDefault(currentRecipe, rowIndex, newActionId);
            return Analyze(newRecipe);
        }

        public Result<RecipeUpdateResult> UpdateStepProperty(Recipe currentRecipe, int rowIndex, ColumnIdentifier key, object value)
        {
            var updateResult = _recipeEngine.UpdateStepProperty(currentRecipe, rowIndex, key, value);

            if (updateResult.IsFailed)
            {
                return updateResult.ToResult<RecipeUpdateResult>();
            }

            var analysisResult = Analyze(updateResult.Value);
            return Result.Ok(analysisResult);
        }

        private RecipeUpdateResult Analyze(Recipe recipe)
        {
            var loopResult = _recipeLoopValidator.Validate(recipe);
            var timeResult = _recipeTimeCalculator.Calculate(recipe);
            return new RecipeUpdateResult(recipe, loopResult, timeResult);
        }
    }
}