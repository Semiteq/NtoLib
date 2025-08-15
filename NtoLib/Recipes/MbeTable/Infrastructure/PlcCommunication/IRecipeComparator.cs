using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PlcCommunication;

/// <summary>
/// Defines the interface for comparing two recipes represented as lists of steps.
/// </summary>
public interface IRecipeComparator
{
    /// <summary>
    /// Compares two recipes, represented as lists of steps, to determine if they are equivalent.
    /// </summary>
    /// <param name="recipe1">The first recipe, represented as a list of steps.</param>
    /// <param name="recipe2">The second recipe, represented as a list of steps.</param>
    /// <returns>True if the recipes are equivalent; otherwise, false.</returns>
    bool Compare(List<Step> recipe1, List<Step> recipe2);
}