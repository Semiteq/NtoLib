using System;
using System.Windows.Forms;

using NtoLib.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.MbeTable.ModulePresentation.Mapping;
using NtoLib.MbeTable.ModulePresentation.Style;

namespace NtoLib.MbeTable.ModulePresentation.Columns;

public abstract class FactoryColumnBase : IFactoryColumn
{
	private const int DefaultMinWidth = 50;
	private const int CriticalMinWidth = 2;

	private readonly IColumnAlignmentResolver _alignmentResolver;

	protected FactoryColumnBase(IColumnAlignmentResolver alignmentResolver)
	{
		_alignmentResolver = alignmentResolver ?? throw new ArgumentNullException(nameof(alignmentResolver));
	}

	public DataGridViewColumn CreateColumn(ColumnDefinition definition, ColorScheme scheme)
	{
		var column = CreateColumnInstance(definition);

		column.Name = definition.Key.Value;
		column.DataPropertyName = definition.Key.Value;
		column.HeaderText = definition.UiName;
		column.SortMode = DataGridViewColumnSortMode.NotSortable;
		column.ReadOnly = definition.ReadOnly;
		column.DefaultCellStyle.Alignment = _alignmentResolver.Resolve(definition);
		column.DefaultCellStyle.Font = scheme.LineFont;
		column.DefaultCellStyle.BackColor = scheme.LineBgColor;
		column.DefaultCellStyle.ForeColor = scheme.LineTextColor;
		column.MinimumWidth = definition.MinimalWidth;
		column.Width = definition.Width;
		column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

		if (definition.MinimalWidth < CriticalMinWidth)
			column.MinimumWidth = DefaultMinWidth;
		if (definition.Width < CriticalMinWidth)
			column.Width = definition.MinimalWidth;
		if (definition.Width == -1)
			column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

		ConfigureColumn(column);
		return column;
	}

	protected abstract DataGridViewColumn CreateColumnInstance(ColumnDefinition definition);

	protected virtual void ConfigureColumn(DataGridViewColumn column)
	{
	}
}
