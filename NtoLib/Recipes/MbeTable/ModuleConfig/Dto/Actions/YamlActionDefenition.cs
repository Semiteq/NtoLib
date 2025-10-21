using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Actions;

/// <summary>
/// DTO for deserializing a single action definition from YAML.
/// </summary>
public sealed class YamlActionDefinition
{
    /// <summary>
    /// Gets or sets the unique numeric action ID.
    /// </summary>
    public short Id { get; set; }

    /// <summary>
    /// Gets or sets the human-readable name of the action.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of deploy duration ("Immediate", "LongLasting", etc.).
    /// </summary>
    public string DeployDuration { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of columns used in this action.
    /// </summary>
    public List<YamlActionColumn> Columns { get; set; } = new List<YamlActionColumn>();
}