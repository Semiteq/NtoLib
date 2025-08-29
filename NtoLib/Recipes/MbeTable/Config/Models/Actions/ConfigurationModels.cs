#nullable enable

namespace NtoLib.Recipes.MbeTable.Config.Models.Actions
{
    /// <summary>
    /// Represents the definition of a calculation rule for dependent step properties.
    /// This is a DTO class used for deserialization from configuration files.
    /// </summary>
    public sealed class CalculationRuleDefinition
    {
        /// <summary>
        /// Gets or sets the unique name of the rule, corresponding to a registered implementation.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the mapping of rule parameters to column keys for this specific action.
        /// </summary>
        public CalculationRuleMapping Mapping { get; set; } = new();
    }
}