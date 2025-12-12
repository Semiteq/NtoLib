using System.Drawing;

using NtoLib.Devices.Helpers;
using NtoLib.Devices.Render.Common;
using NtoLib.Devices.Valves;

namespace NtoLib.Devices.Render.Valves;

internal class ManualValveRenderer : CommonValveRenderer
{
	public ManualValveRenderer(ValveControl valveControl) : base(valveControl)
	{
	}

	public override Bounds Draw(Graphics graphics, RectangleF boundsRect, Orientation orientation, bool isLight)
	{
		var graphicsBounds = base.Draw(graphics, boundsRect, orientation, isLight);
		DrawManualHandle(graphics, graphicsBounds);
		return graphicsBounds;
	}

	/// <summary>
	/// Отрисовывает индикатор ручного привода в виде буквы T, если клапан в ручном режиме
	/// </summary>
	private void DrawManualHandle(Graphics graphics, Bounds graphicsBounds)
	{
		var status = Control.Status;
		if (!status.Manual)
		{
			return;
		}

		var valveBounds = graphicsBounds;
		var offset = 2f * (ErrorLineWidth + ErrorOffset);
		valveBounds.Width -= offset;
		valveBounds.Height -= offset;

		var relativeSize = 0.5f;
		var tHeight = valveBounds.Height * relativeSize;
		if (tHeight <= 0)
		{
			return;
		}

		// Область под T, центрированная относительно клапана
		var tBounds = valveBounds;
		tBounds.Y -= (valveBounds.Height - tHeight) / 2f;
		tBounds.Width = tHeight;
		tBounds.Height = tHeight;

		var center = tBounds.Center;
		var barThickness = tHeight * 0.15f;

		// Вертикальная часть T
		var verticalRect = new RectangleF(
			center.X - barThickness / 2f,
			center.Y + tHeight / 2f,
			barThickness,
			tHeight);

		// Горизонтальная перекладина T той же толщины, что и вертикальная
		var horizontalRect = new RectangleF(
			center.X - tHeight / 2f,
			verticalRect.Bottom,
			tHeight,
			barThickness);

		using var brush = new SolidBrush(Colors.Lines);
		graphics.FillRectangle(brush, verticalRect);
		graphics.FillRectangle(brush, horizontalRect);
	}
}
