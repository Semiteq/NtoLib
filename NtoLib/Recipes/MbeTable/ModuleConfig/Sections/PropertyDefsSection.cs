using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Sections;

/// <summary>
/// Container for property definitions loaded from PropertyDefs.yaml.
/// </summary>
public sealed record PropertyDefsSection(IReadOnlyList<YamlPropertyDefinition> Items);