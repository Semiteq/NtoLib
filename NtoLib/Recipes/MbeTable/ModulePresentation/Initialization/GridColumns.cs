using System.Collections.Generic;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Columns;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Initialization;

public static class GridColumns
{
	public static void Init(
		DataGridView grid,
		IReadOnlyList<ColumnDefinition> columnDefinitions,
		FactoryColumnRegistry registry)
	{
		grid.RowHeadersWidth = 40;

		grid.Columns.Clear();

		foreach (var def in columnDefinitions)
		{
			var column = registry.CreateColumn(def);
			grid.Columns.Add(column);
		}
	}
}
