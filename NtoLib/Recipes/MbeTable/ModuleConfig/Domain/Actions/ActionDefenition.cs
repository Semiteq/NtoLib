using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;

/// <summary>
/// Represents the complete definition for a single recipe action, loaded from configuration.
/// </summary>
public sealed class ActionDefinition
{
    /// <summary>
    /// Unique numeric identifier for the action.
    /// </summary>
    public short Id { get; set; }

    /// <summary>
    /// Display name of the action for the UI.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of action-specific columns (each with its own key, type, defaults, and optional GroupName).
    /// </summary>
    public IReadOnlyList<PropertyConfig> Columns { get; set; } = new List<PropertyConfig>();

    /// <summary>
    /// Deployment duration type for the action.
    /// </summary>
    public DeployDuration DeployDuration { get; set; }
}