using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns.Text;

/// <summary>
/// Read-only StepStartTime column.
/// </summary>
public sealed class StepStartTime : FactoryColumnBase
{
    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition definition) =>
        new DataGridViewTextBoxColumn();

    protected override void ConfigureColumn(DataGridViewColumn column)
    {
        column.DataPropertyName = nameof(StepViewModel.StepStartTime);
        column.ReadOnly = true;
    }
}