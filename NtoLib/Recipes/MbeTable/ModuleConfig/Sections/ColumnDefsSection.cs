using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Columns;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Sections;

/// <summary>
/// Container for column definitions loaded from ColumnDefs.yaml.
/// </summary>
public sealed record ColumnDefsSection(IReadOnlyList<YamlColumnDefinition> Items);