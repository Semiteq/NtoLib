using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Presentation.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Columns;

/// <summary>
/// Read-only StepStartTime column.
/// </summary>
public sealed class StepStartTimeColumnFactory : BaseColumnFactory
{
    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition definition) =>
        new DataGridViewTextBoxColumn();

    protected override void ConfigureColumn(
        DataGridViewColumn column,
        ColumnDefinition   definition,
        ColorScheme        scheme)
    {
        column.DataPropertyName = nameof(StepViewModel.StepStartTime);
        column.ReadOnly         = true;
    }
}