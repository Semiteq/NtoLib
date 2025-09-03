#nullable enable

using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Entities;

/// <summary>
/// Represents a single, immutable step in a recipe. A step consists of a collection of properties.
/// </summary>
public sealed record Step
{
    /// <summary>
    /// A dictionary of all properties for this step, keyed by their column identifier.
    /// Properties not applicable to this step's action have a null value.
    /// </summary>
    public ImmutableDictionary<ColumnIdentifier, StepProperty?> Properties { get; init; }
        
    /// <summary>
    /// The deployment duration type of the action associated with this step.
    /// </summary>
    public DeployDuration DeployDuration { get; init; }

    public Step(
        ImmutableDictionary<ColumnIdentifier, StepProperty?> properties, 
        DeployDuration deployDuration)
    {
        Properties = properties;
        DeployDuration = deployDuration;
    }
}