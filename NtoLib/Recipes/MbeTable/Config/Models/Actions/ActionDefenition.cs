#nullable enable
namespace NtoLib.Recipes.MbeTable.Config.Models.Actions;

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;

/// <summary>
/// Represents the complete definition for a single recipe action, loaded from configuration.
/// </summary>
public sealed class ActionDefinition
{
    /// <summary>
    /// Unique numeric identifier for the action.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name of the action for the UI.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of action-specific columns (each with its own key, type, defaults, and optional GroupName).
    /// </summary>
    public IReadOnlyList<ActionColumnDefinition> Columns { get; set; } = new List<ActionColumnDefinition>();

    /// <summary>
    /// Optional definition for a calculation rule.
    /// </summary>
    public CalculationRuleDefinition? CalculationRule { get; set; }

    /// <summary>
    /// Deployment duration type for the action.
    /// </summary>
    public DeployDuration DeployDuration { get; set; }
}