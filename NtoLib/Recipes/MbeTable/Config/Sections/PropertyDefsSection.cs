

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Dto.Properties;

namespace NtoLib.Recipes.MbeTable.Config.Sections;

/// <summary>
/// Container for property definitions loaded from PropertyDefs.yaml.
/// </summary>
public sealed record PropertyDefsSection(IReadOnlyList<YamlPropertyDefinition> Items);