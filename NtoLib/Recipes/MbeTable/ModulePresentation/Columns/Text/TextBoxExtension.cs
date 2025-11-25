using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Mapping;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns.Text;

public sealed class TextBoxExtension : FactoryColumnBase
{
	public TextBoxExtension(IColumnAlignmentResolver alignmentResolver)
		: base(alignmentResolver)
	{
	}

	protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition definition) =>
		new DataGridViewTextBoxColumn();
}
