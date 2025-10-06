

using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Entities;

namespace NtoLib.Recipes.MbeTable.Csv.Validation;

/// <summary>
/// Validates Recipe objects for correctness and consistency.
/// </summary>
public interface IRecipeValidator
{
    /// <summary>
    /// Validates recipe structure.
    /// </summary>
    /// <param name="recipe">Recipe to validate.</param>
    /// <returns>Validation result.</returns>
    Result ValidateStructure(Recipe recipe);
    
    /// <summary>
    /// Validates loop constructs in the recipe.
    /// </summary>
    /// <param name="recipe">Recipe to validate.</param>
    /// <returns>Validation result.</returns>
    Result ValidateLoops(Recipe recipe);
    
    /// <summary>
    /// Validates target availability for the recipe.
    /// </summary>
    /// <param name="recipe">Recipe to validate.</param>
    /// <returns>Validation result.</returns>
    Result ValidateTargets(Recipe recipe);
    
    /// <summary>
    /// Performs all validations.
    /// </summary>
    /// <param name="recipe">Recipe to validate.</param>
    /// <returns>Combined validation result.</returns>
    Result ValidateAll(Recipe recipe);
}