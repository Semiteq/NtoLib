#nullable enable

using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.RecipeManager.PropertyDataType;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.RecipeManager.StepManager
{
    /// <summary>
    /// Represents an immutable snapshot of a single step in a recipe.
    /// </summary>
    /// <param name="Properties">
    /// An immutable dictionary of all properties within the step.
    /// A key's value can be 'null' to indicate a "blocked" or unavailable property for this step.
    /// </param>
    /// <param name="NestingLevel">The nesting level for loop structures (For/EndFor).</param>
    public record Step(IImmutableDictionary<ColumnKey, StepProperty?> Properties, DeployDuration DeployDuration);
}