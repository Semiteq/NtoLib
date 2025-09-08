#nullable enable

using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Presentation.Table.Cells;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

/// <summary>
/// Creates a read-only text column for displaying the step start time.
/// Visual styling is applied centrally by TableBehaviorManager.
/// </summary>
public class StepStartTimeColumnFactory : BaseColumnFactory
{
    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition colDef)
    {
        return new DataGridViewTextBoxColumn();
    }

    protected override void ConfigureColumn(DataGridViewColumn column, ColumnDefinition colDef, ColorScheme colorScheme)
    {
        column.DataPropertyName = nameof(StepViewModel.StepStartTime);
        column.ReadOnly = true;
        column.CellTemplate = new ReadonlyLabelCell();
    }
}