

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Dto.Actions;

namespace NtoLib.Recipes.MbeTable.Config.Sections;

/// <summary>
/// Container for action definitions loaded from ActionsDefs.yaml.
/// </summary>
public sealed record ActionDefsSection(IReadOnlyList<YamlActionDefinition> Items);