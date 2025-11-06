using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Mapping;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns.Text;

/// <summary>
/// Read-only StepStartTime column.
/// </summary>
public sealed class StepStartTime : FactoryColumnBase
{
    public StepStartTime(IColumnAlignmentResolver alignmentResolver) : base(alignmentResolver)
    {}
    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition definition) =>
        new DataGridViewTextBoxColumn();

    protected override void ConfigureColumn(DataGridViewColumn column)
    {
        column.DataPropertyName = nameof(StepViewModel.StepStartTime);
        column.ReadOnly = true;
    }
}