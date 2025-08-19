using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis;

public interface IRecipeLoopValidator
{
    /// <summary>
    /// Calculates the nesting level for each step and validates the overall loop structure.
    /// </summary>
    /// <param name="recipe">The recipe to analyze.</param>
    /// <returns>A result object containing nesting levels or a validation error.</returns>
    LoopValidationResult Validate(Recipe recipe);
}