using System.Windows.Forms;

using NtoLib.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.MbeTable.ModulePresentation.Mapping;

namespace NtoLib.MbeTable.ModulePresentation.Columns.Text;

public sealed class TextBoxExtension : FactoryColumnBase
{
	public TextBoxExtension(IColumnAlignmentResolver alignmentResolver)
		: base(alignmentResolver)
	{
	}

	protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition definition) =>
		new DataGridViewTextBoxColumn();
}
