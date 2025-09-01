#nullable enable

using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

/// <summary>
/// Default column factory for StepProperty float values.
/// </summary>
public class PropertyColumnFactory : BaseColumnFactory
{
    /// <inheritdoc />
    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition colDef)
    {
        return new PropertyGridColumn();
    }

    /// <inheritdoc />
    protected override void ConfigureColumn(DataGridViewColumn column, ColumnDefinition colDef, ColorScheme colorScheme)
    {
        column.ValueType = colDef.SystemType;
    }
}