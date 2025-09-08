#nullable enable
namespace NtoLib.Recipes.MbeTable.Config.Models.Actions;

using System.Text.Json;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

/// <summary>
/// Defines a single column that is applicable for a specific action,
/// including its semantic type, optional default value, and an optional
/// pin-group data source when PropertyType is Enum.
/// </summary>
public sealed class ActionColumnDefinition
{
    /// <summary>
    /// The column key (must match a Key in TableSchema.json).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Defines the semantic property type (e.g., Enum, Temp, Time, Percent, etc.).
    /// </summary>
    public PropertyType PropertyType { get; set; }

    /// <summary>
    /// Optional default value to initialize new steps. Can be "0" as string in JSON — StepBuilder will convert it.
    /// </summary>
    public JsonElement? DefaultValue { get; set; }

    /// <summary>
    /// Optional pin group name that provides enum options for this column.
    /// Required when PropertyType == Enum and the values are sourced from pin groups.
    /// </summary>
    public string? GroupName { get; set; }
}