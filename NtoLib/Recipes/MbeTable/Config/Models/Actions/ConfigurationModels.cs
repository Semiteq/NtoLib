#nullable enable

namespace NtoLib.Recipes.MbeTable.Config.Models.Actions;

/// <summary>
/// Represents the definition of a calculation rule for dependent step properties.
/// This is a DTO class for deserialization purposes.
/// </summary>
public abstract record CalculationRuleDefinition
{
    public string Name { get; set; } = string.Empty;
    public CalculationRuleMapping Mapping { get; set; } = new();
}