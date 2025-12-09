using System.Drawing;
using System.Drawing.Drawing2D;

using NtoLib.Devices.Helpers;
using NtoLib.Devices.Render.Common;
using NtoLib.Devices.Valves;

namespace NtoLib.Devices.Render.Valves;

internal class CommonValveRenderer : ValveBaseRenderer
{
	public CommonValveRenderer(ValveControl valveControl) : base(valveControl)
	{
		LineWidth = 2f;

		ErrorLineWidth = 2f;
		ErrorOffset = 2f;
	}

	public override Bounds Draw(Graphics graphics, RectangleF boundsRect, Orientation orientation, bool isLight)
	{
		var graphicsBounds = ConfigureGraphics(graphics, boundsRect, orientation);

		var status = Control.Status;
		var errorBounds = GetErrorBounds(graphicsBounds);
		var valveBounds = GetValveBounds(errorBounds);

		if (IsBlocked(status))
			DrawBlockRectangle(graphics, errorBounds);

		DrawValve(graphics, valveBounds, status, isLight);

		if (status.AnyError)
			DrawErrorRectangle(graphics, errorBounds, isLight);

		return graphicsBounds;
	}

	/// <summary>
	/// Отрисовывает клапан, состоящий из двух треугольников в заданной области
	/// </summary>
	private void DrawValve(Graphics graphics, Bounds valveBounds, Status status, bool isLight)
	{
		var colors = GetValveColors(status, isLight);
		var valvePoints = GetValvePoints(valveBounds);
		for (var i = 0; i < valvePoints.Length; i++)
		{
			using var brush = new SolidBrush(colors[i]);
			graphics.FillClosedCurve(brush, valvePoints[i], FillMode.Alternate, 0);
		}

		using var pen = new Pen(Colors.Lines, LineWidth);
		graphics.DrawClosedCurve(pen, valvePoints[0], 0, FillMode.Alternate);
		graphics.DrawClosedCurve(pen, valvePoints[1], 0, FillMode.Alternate);
	}

	/// <summary>
	/// Возвращет границы, в которых должна быть отрисована рамка ошибки
	/// или прямоугольник блокировки
	/// </summary>
	protected static Bounds GetErrorBounds(Bounds graphicsBounds)
	{
		//errorBounds.Height *= 0.8f;
		return graphicsBounds;
	}

	/// <summary>
	/// Возвращает границы, в которых должен быть отрисован клапан/шибер
	/// </summary>
	protected Bounds GetValveBounds(Bounds errorBounds)
	{
		var valveBounds = errorBounds;

		var offset = 2f * (ErrorLineWidth + ErrorOffset);
		valveBounds.Width -= offset;
		valveBounds.Height -= offset;

		return valveBounds;
	}

	/// <summary>
	/// Возвращает массивы точек для двух треугольников клапана соответственно
	/// </summary>
	private PointF[][] GetValvePoints(Bounds valveBounds)
	{
		valveBounds.Width -= LineWidth;
		valveBounds.Height -= LineWidth * 1.154f;

		var points = new PointF[2][];
		points[0] = new[]
		{
			valveBounds.LeftTop,
			valveBounds.Center,
			valveBounds.LeftBottom
		};
		points[1] = new[]
		{
			valveBounds.RightTop,
			valveBounds.Center,
			valveBounds.RightBottom
		};

		return points;
	}
}
