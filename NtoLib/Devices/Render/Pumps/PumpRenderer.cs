using System.Drawing;
using System.Drawing.Drawing2D;

using NtoLib.Devices.Helpers;
using NtoLib.Devices.Pumps;
using NtoLib.Devices.Render.Common;

namespace NtoLib.Devices.Render.Pumps;

public class PumpRenderer : BaseRenderer
{
	/// <summary>Экземпляр ValveControl, к которому привязан данный Renderer</summary>
	private PumpControl _control;

	public PumpRenderer(PumpControl control)
	{
		_control = control;

		LineWidth = 2f;

		ErrorLineWidth = 2f;
		ErrorOffset = 2f;
	}

	public override Bounds Draw(Graphics graphics, RectangleF boundsRect, Orientation orientation, bool isLight)
	{
		var graphicsBounds = ConfigureGraphics(graphics, boundsRect, orientation);

		var status = _control.Status;
		var errorBounds = graphicsBounds;
		var pumpBounds = GetPumpBounds(errorBounds);
		var pumpRect = pumpBounds.ToRectangleF();
		var triangleBounds = GetMainTriangleBounds(pumpBounds);

		if (IsBlocked(status))
			DrawBlockRectangle(graphics, errorBounds);

		var pumpColor = GetCircleColor(status);
		var triangleColors = GetPumpTriangleColors(status, isLight);
		var points = GetPumpTrianglePoints(triangleBounds);

		using (var brush = new SolidBrush(pumpColor))
			graphics.FillEllipse(brush, pumpRect);
		using (var brush = new SolidBrush(triangleColors[0]))
			graphics.FillClosedCurve(brush, points[0], FillMode.Alternate, 0);
		using (var brush = new SolidBrush(triangleColors[1]))
			graphics.FillClosedCurve(brush, points[1], FillMode.Alternate, 0);


		var trianglePoints = GetMainTrianglePoints(triangleBounds);
		using (var pen = new Pen(Colors.Lines, LineWidth))
		{
			graphics.DrawEllipse(pen, pumpRect);
			graphics.DrawClosedCurve(pen, trianglePoints, 0, FillMode.Alternate);
		}

		if (status.Use)
		{
			if (status.AnyError)
				DrawErrorRectangle(graphics, errorBounds, isLight);

			if (status.Warning)
				DrawWarningRectangle(graphics, errorBounds);
		}

		return graphicsBounds;
	}

	/// <summary>
	/// Возвращает границы, в которых должен быть отрисован клапан/шибер
	/// </summary>
	private Bounds GetPumpBounds(Bounds errorBounds)
	{
		var valveBounds = errorBounds;

		var offset = 2f * (ErrorLineWidth + ErrorOffset);
		valveBounds.Width -= offset;
		valveBounds.Height -= offset;

		return valveBounds;
	}

	/// <summary>
	/// Возвращает границы треугольника
	/// </summary>
	private Bounds GetMainTriangleBounds(Bounds pumpBounds)
	{
		var triangleBounds = pumpBounds;

		triangleBounds.Width = triangleBounds.Width / 2f - LineWidth;
		triangleBounds.Height -= 2 * LineWidth;
		triangleBounds.X += triangleBounds.Width / 2f;

		return triangleBounds;
	}

	/// <summary>
	/// Возвращает массив точек треугольника насоса
	/// </summary>
	private static PointF[] GetMainTrianglePoints(Bounds mainTiangleBounds)
	{
		return new[]
		{
			mainTiangleBounds.LeftTop,
			mainTiangleBounds.RightCenter,
			mainTiangleBounds.LeftBottom
		};
	}

	/// <summary>
	/// Возвращает массивы точек для закрашивания двух половинов треугольника
	/// </summary>
	private static PointF[][] GetPumpTrianglePoints(Bounds mainTiangleBounds)
	{
		var points = new PointF[2][];
		points[0] = new[]
		{
			mainTiangleBounds.LeftTop,
			mainTiangleBounds.RightCenter,
			mainTiangleBounds.LeftCenter
		};
		points[1] = new[]
		{
			mainTiangleBounds.LeftBottom,
			mainTiangleBounds.RightCenter,
			mainTiangleBounds.LeftCenter
		};

		return points;
	}

	/// <summary>
	/// Возвращает цвета первой и второй половины треугольника в зависимости от статуса
	/// </summary>
	private static Color[] GetPumpTriangleColors(Status status, bool isLight)
	{
		var colors = new Color[2];

		if (!status.Use || !status.ConnectionOk)
		{
			colors[0] = Colors.NoDataDark;
			colors[1] = Colors.NoDataDark;
		}
		else if (status.WorkOnNominalSpeed)
		{
			colors[0] = Colors.Opened;
			colors[1] = Colors.Opened;
		}
		else if (status.Stopped)
		{
			colors[0] = Colors.Closed;
			colors[1] = Colors.Closed;
		}
		else if (status.Accelerating)
		{
			colors[0] = Colors.Closed;
			colors[1] = isLight ? Colors.Opened : Colors.Closed;
		}
		else if (status.Decelerating)
		{
			colors[0] = isLight ? Colors.Opened : Colors.Closed;
			colors[1] = Colors.Opened;
		}

		return colors;
	}

	private static Color GetCircleColor(Status status)
	{
		if (!status.Use || !status.ConnectionOk)
		{
			return Colors.NoData;
		}
		else
		{
			return Colors.Closed;
		}
	}

	/// <summary>
	/// Возвращает истину, если должна быть показана блокировка
	/// </summary>
	private static bool IsBlocked(Status status)
	{
		if ((status.Stopped || status.Decelerating) && (status.BlockStart || status.ForceStop))
			return true;

		return (status.WorkOnNominalSpeed || status.Accelerating) && status.BlockStop;
	}
}
