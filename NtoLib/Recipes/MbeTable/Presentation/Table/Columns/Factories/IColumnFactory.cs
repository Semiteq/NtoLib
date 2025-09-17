using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

public interface IColumnFactory
{
    DataGridViewColumn CreateColumn(ColumnDefinition colDef, ColorScheme colorScheme);
}