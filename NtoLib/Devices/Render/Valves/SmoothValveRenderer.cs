using System.Drawing;

using NtoLib.Devices.Helpers;
using NtoLib.Devices.Render.Common;
using NtoLib.Devices.Valves;

namespace NtoLib.Devices.Render.Valves;

internal class SmoothValveRenderer : CommonValveRenderer
{
	/// <summary>Диаметр кружочка относительно высоты клапана</summary>
	private const float RelativeCircleDiameter = 0.3f;

	public SmoothValveRenderer(ValveControl valveControl) : base(valveControl)
	{
	}

	public override Bounds Draw(Graphics graphics, RectangleF boundsRect, Orientation orientation, bool isLight)
	{
		var graphicsBounds = base.Draw(graphics, boundsRect, orientation, isLight);

		var status = Control.Status;
		var errorBounds = GetErrorBounds(graphicsBounds);
		var valveBounds = GetValveBounds(errorBounds);

		DrawSmoothValveCircle(graphics, valveBounds, status);

		return graphicsBounds;
	}

	/// <summary>
	/// Отрисовывает кружочек (и ножку) клапана плавной откачки
	/// </summary>
	private void DrawSmoothValveCircle(Graphics graphics, Bounds valveBounds, Status status)
	{
		var circleBounds = GetCircleBounds(valveBounds);
		var circleRect = circleBounds.ToRectangleF();
		using (Brush brush = new SolidBrush(GetCircleColor(status)))
		{
			graphics.FillEllipse(brush, circleRect);
		}

		using (var pen = new Pen(Colors.Lines, LineWidth))
		{
			graphics.DrawEllipse(pen, circleRect);

			var legPoints = GetCircleLegPoints(valveBounds, circleBounds);
			graphics.DrawLines(pen, legPoints);
		}
	}

	/// <summary>
	/// Возвращает границы, в которых должен быть отрисован кружочек клапана плавной откачки
	/// </summary>
	private static Bounds GetCircleBounds(Bounds valveBounds)
	{
		var circleBounds = valveBounds;

		var circleDiameter = valveBounds.Height * RelativeCircleDiameter;
		circleBounds.Y -= (valveBounds.Height - circleDiameter) / 2f;
		circleBounds.Width = circleDiameter;
		circleBounds.Height = circleDiameter;

		return circleBounds;
	}

	/// <summary>
	/// Возвращает точки для отрисовки по ним ножки кружочка клапана плавной откачки
	/// </summary>
	private static PointF[] GetCircleLegPoints(Bounds valveBounds, Bounds circleBounds)
	{
		return new[] { valveBounds.Center, circleBounds.CenterBottom };
	}

	/// <summary>
	/// Возвращает цвет кружочка клапана плавной откачки в зависимости от состояния
	/// </summary>
	private static Color GetCircleColor(Status status)
	{
		Color color;

		if (!status.ConnectionOk)
		{
			color = Colors.NoDataDark;
		}
		else if (status.WithoutSensors)
		{
			color = Colors.NoDataDark;
		}
		else if (status.Opened || status.OpenedSmoothly)
		{
			color = Colors.Opened;
		}
		else
		{
			color = Colors.Closed;
		}

		return color;
	}
}
