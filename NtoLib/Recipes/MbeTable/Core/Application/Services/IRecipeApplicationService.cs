#nullable enable

using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Core.Application.Services;

/// <summary>
/// Defines the contract for the recipe application service.
/// </summary>
public interface IRecipeApplicationService
{
    RecipeUpdateResult CreateEmpty();
    RecipeUpdateResult AddDefaultStep(Recipe currentRecipe, int rowIndex);
    RecipeUpdateResult RemoveStep(Recipe currentRecipe, int rowIndex);
    Result<RecipeUpdateResult> UpdateStepProperty(Recipe currentRecipe, int rowIndex, ColumnIdentifier key, object value);
    RecipeUpdateResult ReplaceStepWithNewDefault(Recipe currentRecipe, int rowIndex, int newActionId);
    
    /// <summary>
    /// Analyzes a complete recipe object to produce loop and time analysis results.
    /// </summary>
    /// <param name="recipe">The recipe to analyze.</param>
    /// <returns>A result object containing the original recipe and its analysis data.</returns>
    RecipeUpdateResult AnalyzeRecipe(Recipe recipe);
}