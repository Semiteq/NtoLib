using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Actions;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Sections;

/// <summary>
/// Container for action definitions loaded from ActionsDefs.yaml.
/// </summary>
public sealed record ActionDefsSection(IReadOnlyList<YamlActionDefinition> Items);