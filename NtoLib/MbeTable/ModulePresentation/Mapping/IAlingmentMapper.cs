using System.Windows.Forms;

using NtoLib.MbeTable.ModuleConfig.Dto.Columns;

namespace NtoLib.MbeTable.ModulePresentation.Mapping;

public interface IAlignmentMapper
{
	DataGridViewContentAlignment Map(UiAlignment value);
}
