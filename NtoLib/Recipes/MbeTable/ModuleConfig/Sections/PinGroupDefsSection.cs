using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.PinGroups;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Sections;

/// <summary>
/// Container for pin group definitions loaded from PinGroupDefs.yaml.
/// </summary>
public sealed record PinGroupDefsSection(IReadOnlyList<YamlPinGroupDefinition> Items);