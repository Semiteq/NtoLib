using System.Windows.Forms;

using NtoLib.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.MbeTable.ModulePresentation.Mapping;

public interface IColumnAlignmentResolver
{
	DataGridViewContentAlignment Resolve(ColumnDefinition column);
}
