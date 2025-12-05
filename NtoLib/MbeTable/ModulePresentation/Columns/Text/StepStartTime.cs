using System.Windows.Forms;

using NtoLib.MbeTable.ModuleApplication.ViewModels;
using NtoLib.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.MbeTable.ModulePresentation.Mapping;

namespace NtoLib.MbeTable.ModulePresentation.Columns.Text;

/// <summary>
/// Read-only StepStartTime column.
/// </summary>
public sealed class StepStartTime : FactoryColumnBase
{
	public StepStartTime(IColumnAlignmentResolver alignmentResolver) : base(alignmentResolver)
	{
	}

	protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition definition) =>
		new DataGridViewTextBoxColumn();

	protected override void ConfigureColumn(DataGridViewColumn column)
	{
		column.DataPropertyName = nameof(StepViewModel.StepStartTime);
		column.ReadOnly = true;
	}
}
