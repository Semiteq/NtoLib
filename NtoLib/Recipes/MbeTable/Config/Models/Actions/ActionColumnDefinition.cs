#nullable enable

using System.Text.Json;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

namespace NtoLib.Recipes.MbeTable.Config.Models.Actions
{
    /// <summary>
    /// Defines the properties of a column as used by a specific action.
    /// This is a DTO class used for deserialization from configuration files.
    /// </summary>
    public sealed class ActionColumnDefinition
    {
        /// <summary>
        /// Gets or sets the semantic type of the property for this action (e.g., Temp, Time, Percent).
        /// </summary>
        public PropertyType PropertyType { get; set; }

        /// <summary>
        /// Gets or sets the default value for this property when a new step of this action is created.
        /// Can be null if no default value is specified.
        /// </summary>
        public JsonElement? DefaultValue { get; set; }
    }
}