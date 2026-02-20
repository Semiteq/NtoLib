using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Mapping;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns;

public abstract class FactoryColumnBase : IFactoryColumn
{
	public DataGridViewColumn CreateColumn(ColumnDefinition definition, ColorScheme scheme)
	{
		var column = CreateColumnInstance(definition);
		ColumnStyleMapper.Map(column, definition, scheme);
		ConfigureColumn(column);

		return column;
	}

	protected abstract DataGridViewColumn CreateColumnInstance(ColumnDefinition definition);

	protected virtual void ConfigureColumn(DataGridViewColumn column)
	{
	}
}
