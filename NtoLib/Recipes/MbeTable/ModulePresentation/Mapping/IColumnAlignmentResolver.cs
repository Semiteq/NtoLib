using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Mapping;

public interface IColumnAlignmentResolver
{
	DataGridViewContentAlignment Resolve(ColumnDefinition column);
}
