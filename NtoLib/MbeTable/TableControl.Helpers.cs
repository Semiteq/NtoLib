using System.Drawing;

namespace NtoLib.MbeTable;

public partial class TableControl
{
	private static Color Darken(Color color)
	{
		const int Delta = 40;
		return Color.FromArgb(
			color.A,
			Clamp(color.R - Delta),
			Clamp(color.G - Delta),
			Clamp(color.B - Delta));

		int Clamp(int value) => value < 0 ? 0 : value > 255 ? 255 : value;
	}
}
