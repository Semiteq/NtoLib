namespace NtoLib.Recipes.MbeTable.Config.Models.Actions;

/// <summary>
/// Defines the mapping of step properties to the parameters of a calculation rule.
/// This is a DTO class for deserialization purposes.
/// </summary>
public record CalculationRuleMapping
{
    public string Rate { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Initial { get; set; } = string.Empty;
    public string Final { get; set; } = string.Empty;
}