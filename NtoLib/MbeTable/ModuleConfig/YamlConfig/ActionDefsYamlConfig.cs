using System.Collections.Generic;

using NtoLib.MbeTable.ModuleConfig.Dto.Actions;

namespace NtoLib.MbeTable.ModuleConfig.YamlConfig;

/// <summary>
/// Container for action definitions loaded from ActionsDefs.yaml.
/// </summary>
public sealed record ActionDefsYamlConfig(IReadOnlyList<YamlActionDefinition> Items);
