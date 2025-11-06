using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Columns;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.YamlConfig;

/// <summary>
/// Container for column definitions loaded from ColumnDefs.yaml.
/// </summary>
public sealed record ColumnDefsYamlConfig(IReadOnlyList<YamlColumnDefinition> Items);