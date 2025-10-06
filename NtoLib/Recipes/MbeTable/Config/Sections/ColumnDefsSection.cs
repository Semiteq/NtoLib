

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Dto.Columns;

namespace NtoLib.Recipes.MbeTable.Config.Sections;

/// <summary>
/// Container for column definitions loaded from ColumnDefs.yaml.
/// </summary>
public sealed record ColumnDefsSection(IReadOnlyList<YamlColumnDefinition> Items);