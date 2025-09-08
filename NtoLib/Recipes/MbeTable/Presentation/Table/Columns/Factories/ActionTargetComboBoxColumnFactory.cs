#nullable enable
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

public class ActionTargetComboBoxColumnFactory : BaseColumnFactory
{
    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition colDef)
    {
        return new ActionTargetComboBoxColumn();
    }

    protected override void ConfigureColumn(DataGridViewColumn column, ColumnDefinition colDef, ColorScheme colorScheme)
    {
    }
}