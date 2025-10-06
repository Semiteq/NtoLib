

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Dto.PinGroups;

namespace NtoLib.Recipes.MbeTable.Config.Sections;

/// <summary>
/// Container for pin group definitions loaded from PinGroupDefs.yaml.
/// </summary>
public sealed record PinGroupDefsSection(IReadOnlyList<YamlPinGroupDefinition> Items);