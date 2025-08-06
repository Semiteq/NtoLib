#nullable enable

using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.RecipeManager.StepManager;

namespace NtoLib.Recipes.MbeTable.RecipeManager
{
    /// <summary>
    /// Represents an immutable snapshot of an entire recipe.
    /// </summary>
    /// <param name="Steps">The immutable list of steps that make up the recipe.</param>
    public record Recipe(IImmutableList<Step> Steps);
}