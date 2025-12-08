using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Columns;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Mapping;

public sealed class AlignmentMapper : IAlignmentMapper
{
	public DataGridViewContentAlignment Map(UiAlignment value)
	{
		switch (value)
		{
			case UiAlignment.Left:
				return DataGridViewContentAlignment.MiddleLeft;
			case UiAlignment.Center:
				return DataGridViewContentAlignment.MiddleCenter;
			case UiAlignment.Right:
				return DataGridViewContentAlignment.MiddleRight;
			case UiAlignment.TopLeft:
				return DataGridViewContentAlignment.TopLeft;
			case UiAlignment.TopCenter:
				return DataGridViewContentAlignment.TopCenter;
			case UiAlignment.TopRight:
				return DataGridViewContentAlignment.TopRight;
			case UiAlignment.MiddleLeft:
				return DataGridViewContentAlignment.MiddleLeft;
			case UiAlignment.MiddleCenter:
				return DataGridViewContentAlignment.MiddleCenter;
			case UiAlignment.MiddleRight:
				return DataGridViewContentAlignment.MiddleRight;
			case UiAlignment.BottomLeft:
				return DataGridViewContentAlignment.BottomLeft;
			case UiAlignment.BottomCenter:
				return DataGridViewContentAlignment.BottomCenter;
			case UiAlignment.BottomRight:
				return DataGridViewContentAlignment.BottomRight;
			default:
				return DataGridViewContentAlignment.MiddleCenter;
		}
	}
}
