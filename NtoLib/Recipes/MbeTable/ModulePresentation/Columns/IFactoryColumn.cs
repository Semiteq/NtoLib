using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns;

public interface IFactoryColumn
{
	DataGridViewColumn CreateColumn(ColumnDefinition definition, ColorScheme scheme);
}
