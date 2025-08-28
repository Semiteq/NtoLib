#nullable enable

using System.Collections.Generic;
using System.Text.Json;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;

namespace NtoLib.Recipes.MbeTable.Composition;

/// <summary>
/// Defines the mapping of step properties to the parameters of a calculation rule.
/// This is a DTO class for deserialization purposes.
/// </summary>
public sealed class CalculationRuleMapping
{
    public string Rate { get; set; }
    public string Duration { get; set; }
    public string Initial { get; set; }
    public string Final { get; set; }

    public CalculationRuleMapping()
    {
        Rate = string.Empty;
        Duration = string.Empty;
        Initial = string.Empty;
        Final = string.Empty;
    }
}

/// <summary>
/// Represents the definition of a calculation rule for dependent step properties.
/// This is a DTO class for deserialization purposes.
/// </summary>
public sealed class CalculationRuleDefinition
{
    public string Name { get; set; }
    public CalculationRuleMapping Mapping { get; set; }

    public CalculationRuleDefinition()
    {
        Name = string.Empty;
        Mapping = new CalculationRuleMapping();
    }
}

/// <summary>
/// Represents the complete definition for a single recipe action, loaded from configuration.
/// This is a DTO class for deserialization purposes.
/// </summary>
public sealed class ActionDefinition
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ActionType ActionType { get; set; }
    public IReadOnlyList<string> ApplicableColumns { get; set; }
    public IReadOnlyDictionary<string, JsonElement> DefaultValues { get; set; }
    public CalculationRuleDefinition? CalculationRule { get; set; }
    public DeployDuration DeployDuration { get; set; }

    public ActionDefinition()
    {
        Name = string.Empty;
        // Initialize collections to prevent null reference issues after deserialization.
        ApplicableColumns = new List<string>();
        DefaultValues = new Dictionary<string, JsonElement>();
    }
}

/// <summary>
/// A container for the entire application's configuration, loaded at startup.
/// This can remain a record as it's constructed in code, not deserialized directly.
/// </summary>
/// <param name="Schema">The definition of the table's structure.</param>
/// <param name="Actions">A dictionary of all available actions, keyed by their ID.</param>
public sealed record AppConfiguration(
    TableSchema Schema,
    IReadOnlyDictionary<int, ActionDefinition> Actions
);