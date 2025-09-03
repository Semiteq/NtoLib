#nullable enable

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;

namespace NtoLib.Recipes.MbeTable.Config.Models.Actions
{
    /// <summary>
    /// Represents the complete definition for a single recipe action, loaded from configuration.
    /// This is a DTO class used for deserialization from configuration files.
    /// </summary>
    public sealed class ActionDefinition
    {
        /// <summary>
        /// Gets or sets the unique numeric identifier for the action.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the action for the UI.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the target hardware group this action operates on.
        /// The value must correspond to a 'GroupName' in 'PinGroups.json'.
        /// Can be null if the action does not require a hardware target.
        /// </summary>
        public string? TargetGroup { get; set; }

        /// <summary>
        /// Gets or sets a dictionary defining the columns applicable to this action and their specific properties.
        /// The key is the column's string identifier (e.g., "setpoint").
        /// </summary>
        public IReadOnlyDictionary<string, ActionColumnDefinition> Columns { get; set; } = new Dictionary<string, ActionColumnDefinition>();

        /// <summary>
        /// Gets or sets an optional definition for a calculation rule.
        /// </summary>
        public CalculationRuleDefinition? CalculationRule { get; set; }

        /// <summary>
        /// Gets or sets the deployment duration type for the action.
        /// </summary>
        public DeployDuration DeployDuration { get; set; }
    }
}