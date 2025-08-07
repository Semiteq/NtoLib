using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns;

public interface IColumnFactory
{
    DataGridViewColumn CreateColumn(ColumnDefinition colDef, ColorScheme colorScheme);
}