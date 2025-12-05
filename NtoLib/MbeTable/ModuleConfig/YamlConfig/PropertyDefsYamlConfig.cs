using System.Collections.Generic;

using NtoLib.MbeTable.ModuleConfig.Dto.Properties;

namespace NtoLib.MbeTable.ModuleConfig.YamlConfig;

/// <summary>
/// Container for property definitions loaded from PropertyDefs.yaml.
/// </summary>
public sealed record PropertyDefsYamlConfig(IReadOnlyList<YamlPropertyDefinition> Items);
