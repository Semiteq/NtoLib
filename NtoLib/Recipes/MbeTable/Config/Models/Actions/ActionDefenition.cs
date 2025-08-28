#nullable enable

using System.Collections.Generic;
using System.Text.Json;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;

namespace NtoLib.Recipes.MbeTable.Config.Models.Actions;

/// <summary>
/// Represents the complete definition for a single recipe action, loaded from configuration.
/// This is a DTO class for deserialization purposes.
/// </summary>
public class ActionDefinition
{
    // Initialize collections to prevent null reference issues after deserialization.
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ActionType ActionType { get; set; }
    public IReadOnlyList<string> ApplicableColumns { get; set; } = new List<string>();
    public IReadOnlyDictionary<string, JsonElement> DefaultValues { get; set; } = new Dictionary<string, JsonElement>();
    public CalculationRuleDefinition? CalculationRule { get; set; }
    public DeployDuration DeployDuration { get; set; }
}