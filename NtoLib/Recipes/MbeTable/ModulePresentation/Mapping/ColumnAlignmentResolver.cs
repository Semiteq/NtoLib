using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Mapping;

public sealed class ColumnAlignmentResolver : IColumnAlignmentResolver
{
	private readonly IAlignmentMapper _alignmentMapper;

	public ColumnAlignmentResolver(IAlignmentMapper alignmentMapper)
	{
		_alignmentMapper = alignmentMapper;
	}

	public DataGridViewContentAlignment Resolve(ColumnDefinition column)
	{
		return _alignmentMapper.Map(column.Alignment);
	}
}
