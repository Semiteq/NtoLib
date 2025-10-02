#nullable enable

using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Presentation.Context;
using NtoLib.Recipes.MbeTable.Presentation.Table.Cells;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

/// <summary>
/// Creates a read-only text column for displaying the step start time.
/// Visual styling is applied centrally by TableRenderCoordinator.
/// </summary>
public sealed class StepStartTimeColumnFactory : BaseColumnFactory
{
    public StepStartTimeColumnFactory(IComboBoxContext comboBoxContext)
        : base(comboBoxContext)
    {
    }

    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition columnDefinition)
    {
        return new DataGridViewTextBoxColumn();
    }

    protected override void ConfigureColumn(DataGridViewColumn column, ColumnDefinition columnDefinition, ColorScheme colorScheme)
    {
        column.DataPropertyName = nameof(StepViewModel.StepStartTime);
        column.ReadOnly = true;
        column.CellTemplate = new ReadonlyLabelCell();
    }
}