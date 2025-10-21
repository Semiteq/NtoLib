using System;
using System.Collections.Generic;
using System.Linq;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Domain;

/// <summary>
/// Pre-calculated information about recipe table columns.
/// </summary>
public sealed class RecipeColumnLayout
{
    public RecipeColumnLayout(IReadOnlyList<ColumnDefinition> tableColumns)
    {
        if (tableColumns is null) 
            throw new ArgumentNullException(nameof(tableColumns));
        
        _mappedColumns = tableColumns
            .Where(c => c.PlcMapping is not null)
            .ToArray();

        IntColumnCount = CountForArea("Int");
        FloatColumnCount = CountForArea("Float");
    }

    public int IntColumnCount { get; }
    public int FloatColumnCount { get; }
    public IReadOnlyList<ColumnDefinition> MappedColumns => _mappedColumns;

    private readonly ColumnDefinition[] _mappedColumns;

    private int CountForArea(string area) =>
        (_mappedColumns
            .Where(c => c.PlcMapping!.Area.Equals(area, StringComparison.OrdinalIgnoreCase))
            .Max(c => (int?)c.PlcMapping!.Index) ?? -1) + 1;
}