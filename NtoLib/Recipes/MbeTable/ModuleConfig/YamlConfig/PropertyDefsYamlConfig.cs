using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.YamlConfig;

/// <summary>
/// Container for property definitions loaded from PropertyDefs.yaml.
/// </summary>
public sealed record PropertyDefsYamlConfig(IReadOnlyList<YamlPropertyDefinition> Items);
