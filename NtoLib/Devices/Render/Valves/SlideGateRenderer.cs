using System.Drawing;
using System.Drawing.Drawing2D;

using NtoLib.Devices.Helpers;
using NtoLib.Devices.Render.Common;
using NtoLib.Devices.Valves;

namespace NtoLib.Devices.Render.Valves;

internal class SlideGateRenderer : ValveBaseRenderer
{
	/// <summary>Толщина паза задвижки относительно ширины шибера</summary>
	private const float RelativeGrooveWidth = 0.12f;

	/// <summary>Толщина задвижки относительно внутренней ширины паза</summary>
	private const float RelativeGateWidth = 0.75f;

	public SlideGateRenderer(ValveControl valveControl) : base(valveControl)
	{
		LineWidth = 2f;

		ErrorLineWidth = 2f;
		ErrorOffset = 2f;
	}

	public override Bounds Draw(Graphics graphics, RectangleF boundsRect, Orientation orientation, bool isLight)
	{
		var graphicsBounds = ConfigureGraphics(graphics, boundsRect, orientation);

		var status = Control.Status;
		var valveBounds = GetValveBounds(graphicsBounds);

		if (IsBlocked(status))
		{
			DrawBlockRectangle(graphics, graphicsBounds);
		}

		DrawValve(graphics, valveBounds, status, isLight);
		DrawGrooveAndGate(graphics, graphicsBounds, status, isLight);

		if (status.AnyError)
		{
			DrawErrorRectangle(graphics, graphicsBounds, isLight);
		}

		return graphicsBounds;
	}

	/// <summary>
	/// Отрисовывает клапан, состоящий из двух треугольников в заданной области,
	/// при этом оставляет между треугольниками зазор для задвижки шибера
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
	/// Отрисовывает паз для задвижки в виде прямоугольной рамки
	/// </summary>
	private void DrawGrooveAndGate(Graphics graphics, Bounds errorBounds, Status status, bool isLight)
	{
		var grooveBounds = GetGrooveBounds(errorBounds);
		var groovePoints = grooveBounds.GetPoints(-LineWidth / 2f);

		using (var pen = new Pen(Colors.Lines, LineWidth))
		{
			graphics.DrawClosedCurve(pen, groovePoints, 0, FillMode.Alternate);
		}

		// Для клапанов без датчиков в ручном режиме не показываем положение задвижки
		if (!(status.Manual && status.WithoutSensors))
		{
			var gateBounds = GetGateBounds(grooveBounds, status, isLight);

			using (Brush brush = new SolidBrush(Colors.Lines))
			{
				graphics.FillRectangle(brush, gateBounds.ToRectangleF());
			}
		}
	}

	/// <summary>
	/// Возвращает границы, в которых должен быть отрисован клапан/шибер
	/// </summary>
	private Bounds GetValveBounds(Bounds errorBounds)
	{
		var valveBounds = errorBounds;

		var offset = 2f * (ErrorLineWidth + ErrorOffset);
		valveBounds.Width -= offset;
		valveBounds.Height -= offset;
		valveBounds.Height *= 0.8f;

		return valveBounds;
	}

	/// <summary>
	/// Возвращает массивы точек для двух треугольников шибера соответственно
	/// </summary>
	private PointF[][] GetValvePoints(Bounds valveBounds)
	{
		valveBounds.Width -= LineWidth;
		valveBounds.Height -= LineWidth * 1.154f;

		var offsetFromCenter = valveBounds.Width * RelativeGrooveWidth / 2f + LineWidth / 2f;
		var leftTriangleCenter = new PointF(valveBounds.CenterX - offsetFromCenter, valveBounds.CenterY);
		var rightTriangleCenter = new PointF(valveBounds.CenterX + offsetFromCenter, valveBounds.CenterY);

		var points = new PointF[2][];
		points[0] = new[] { valveBounds.LeftTop, leftTriangleCenter, valveBounds.LeftBottom };
		points[1] = new[] { valveBounds.RightTop, rightTriangleCenter, valveBounds.RightBottom };

		return points;
	}

	/// <summary>
	/// Возвращает границы паза для задвижки шибера
	/// </summary>
	private Bounds GetGrooveBounds(Bounds errorBounds)
	{
		var grooveBounds = errorBounds;

		var offset = 2f * (ErrorLineWidth + ErrorOffset);
		grooveBounds.Height = (errorBounds.Height - offset) * (2f / 3f);
		grooveBounds.Width = errorBounds.Width * RelativeGrooveWidth;
		grooveBounds.Y -= grooveBounds.Height / 4f;

		return grooveBounds;
	}

	/// <summary>
	/// Возвращает границы задвижки шибера
	/// </summary>
	private Bounds GetGateBounds(Bounds grooveBounds, Status status, bool isLight)
	{
		var gateBounds = grooveBounds;

		gateBounds.Width = (grooveBounds.Width - 2f * LineWidth) * RelativeGateWidth;

		var gap = (grooveBounds.Width - 2f * LineWidth - gateBounds.Width) / 2f;
		gateBounds.Height = (grooveBounds.Height - 2f * LineWidth - 3f * gap) / 2f;

		if (status.Opened || (status.OpeningClosing && isLight))
		{
			gateBounds.Y -= grooveBounds.Height / 4f - LineWidth / 2f;
		}
		else if (status.Closed || (status.OpeningClosing && !isLight))
		{
			gateBounds.Y += grooveBounds.Height / 4f - LineWidth / 2f;
		}

		return gateBounds;
	}
}
