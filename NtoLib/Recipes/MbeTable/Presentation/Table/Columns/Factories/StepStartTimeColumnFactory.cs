#nullable enable

using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Presentation.Table.Cells;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

/// <summary>
/// Creates a read-only text column for displaying the step start time.
/// This column uses a custom cell for specific styling and is not user-editable.
/// </summary>
public class StepStartTimeColumnFactory : BaseColumnFactory
{
    /// <inheritdoc />
    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition colDef)
    {
        return new DataGridViewTextBoxColumn();
    }

    /// <inheritdoc />
    protected override void ConfigureColumn(DataGridViewColumn column, ColumnDefinition colDef, ColorScheme colorScheme)
    {
        column.DataPropertyName = nameof(StepViewModel.StepStartTime);
        column.ReadOnly = true;
        column.CellTemplate = new ReadonlyLabelCell();
        
        column.DefaultCellStyle.BackColor =
            colorScheme.BlockedBgColor.IsEmpty ? colorScheme.LineBgColor : colorScheme.BlockedBgColor;
        
        column.DefaultCellStyle.ForeColor = colorScheme.BlockedTextColor.IsEmpty
            ? colorScheme.LineTextColor
            : colorScheme.BlockedTextColor;
    }
}