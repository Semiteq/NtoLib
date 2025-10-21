using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns;

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