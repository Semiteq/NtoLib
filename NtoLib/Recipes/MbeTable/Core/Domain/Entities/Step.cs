#nullable enable

using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Entities
{
    /// <summary>
    /// Represents an immutable snapshot of a single step in a recipe.
    /// </summary>
    /// <param name="Properties">
    /// An immutable dictionary of all properties within the step.
    /// A key's value can be 'null' to indicate a "blocked" or unavailable property for this step.
    /// </param>
    public record Step(IImmutableDictionary<ColumnIdentifier, StepProperty?> Properties, DeployDuration DeployDuration);
}