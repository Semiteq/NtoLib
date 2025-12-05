using System.Collections.Generic;

using NtoLib.MbeTable.ModuleConfig.Dto.PinGroups;

namespace NtoLib.MbeTable.ModuleConfig.YamlConfig;

/// <summary>
/// Container for pin group definitions loaded from PinGroupDefs.yaml.
/// </summary>
public sealed record PinGroupDefsYamlConfig(IReadOnlyList<YamlPinGroupDefinition> Items);
