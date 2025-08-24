using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis;

public interface IRecipeTimeCalculator
{
    /// <summary>
    /// Calculates the total duration and step start times for a given recipe based on its steps
    /// and their associated properties, such as action types, durations, and loops.
    /// </summary>
    /// <param name="recipe">The recipe containing a list of steps to be analyzed for timing information.</param>
    /// <param name="loopResult">The result of loop validation, providing information on the validity and structure of loops within the recipe.</param>
    /// <returns>A <see cref="RecipeTimeAnalysis"/> object containing the total duration and start times of each step in the recipe.</returns>
    RecipeTimeAnalysis Calculate(Recipe recipe);
}