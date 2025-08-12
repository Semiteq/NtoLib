using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

public interface IColumnFactory
{
    DataGridViewColumn CreateColumn(ColumnDefinition colDef, ColorScheme colorScheme);
}