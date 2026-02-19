using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Mapping;

public static class ColumnStyleMapper
{
	private const int DefaultMinWidth = 50;
	private const int CriticalMinWidth = 2;

	public static void Map(DataGridViewColumn column, ColumnDefinition definition, ColorScheme scheme)
	{
		column.Name = definition.Key.Value;
		column.DataPropertyName = definition.Key.Value;
		column.HeaderText = definition.UiName;
		column.SortMode = DataGridViewColumnSortMode.NotSortable;
		column.ReadOnly = definition.ReadOnly;
		column.DefaultCellStyle.Alignment = MapAlignment(definition.Alignment);
		column.DefaultCellStyle.Font = scheme.LineFont;
		column.DefaultCellStyle.BackColor = scheme.LineBgColor;
		column.DefaultCellStyle.ForeColor = scheme.LineTextColor;
		column.MinimumWidth = definition.MinimalWidth;
		column.Width = definition.Width;
		column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

		if (definition.MinimalWidth < CriticalMinWidth)
		{
			column.MinimumWidth = DefaultMinWidth;
		}

		if (definition.Width < CriticalMinWidth)
		{
			column.Width = definition.MinimalWidth;
		}

		if (definition.Width == -1)
		{
			column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
		}
	}

	private static DataGridViewContentAlignment MapAlignment(UiAlignment value)
	{
		return value switch
		{
			UiAlignment.Left => DataGridViewContentAlignment.MiddleLeft,
			UiAlignment.Center => DataGridViewContentAlignment.MiddleCenter,
			UiAlignment.Right => DataGridViewContentAlignment.MiddleRight,
			UiAlignment.TopLeft => DataGridViewContentAlignment.TopLeft,
			UiAlignment.TopCenter => DataGridViewContentAlignment.TopCenter,
			UiAlignment.TopRight => DataGridViewContentAlignment.TopRight,
			UiAlignment.MiddleLeft => DataGridViewContentAlignment.MiddleLeft,
			UiAlignment.MiddleCenter => DataGridViewContentAlignment.MiddleCenter,
			UiAlignment.MiddleRight => DataGridViewContentAlignment.MiddleRight,
			UiAlignment.BottomLeft => DataGridViewContentAlignment.BottomLeft,
			UiAlignment.BottomCenter => DataGridViewContentAlignment.BottomCenter,
			UiAlignment.BottomRight => DataGridViewContentAlignment.BottomRight,
			_ => DataGridViewContentAlignment.MiddleCenter
		};
	}
}
