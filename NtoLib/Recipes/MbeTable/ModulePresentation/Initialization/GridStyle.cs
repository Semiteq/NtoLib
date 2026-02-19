using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Initialization;

public static class GridStyle
{
	public static void Init(DataGridView grid, ColorScheme scheme)
	{
		grid.ColumnHeadersDefaultCellStyle.BackColor = scheme.HeaderBgColor;
		grid.ColumnHeadersDefaultCellStyle.ForeColor = scheme.HeaderTextColor;
		grid.ColumnHeadersDefaultCellStyle.Font = scheme.HeaderFont;

		grid.DefaultCellStyle.BackColor = scheme.LineBgColor;
		grid.DefaultCellStyle.ForeColor = scheme.LineTextColor;
		grid.DefaultCellStyle.Font = scheme.LineFont;
		grid.RowTemplate.Height = scheme.LineHeight;

		grid.BackgroundColor = scheme.TableBackgroundColor;
	}
}
