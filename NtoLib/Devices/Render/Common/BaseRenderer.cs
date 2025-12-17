using System.Drawing;
using System.Drawing.Drawing2D;

using NtoLib.Devices.Helpers;

namespace NtoLib.Devices.Render.Common;

public abstract class BaseRenderer
{
	/// <summary>Толщина всех линий, кроме линии ошибки</summary>
	protected float LineWidth { get; set; }

	/// <summary>Толщина линии ошибки</summary>
	protected float ErrorLineWidth { get; set; }

	/// <summary>Отступ от границ клапана до рамки ошибки</summary>
	protected float ErrorOffset { get; set; }

	/// <summary>
	/// Метод для отрисовки объекта Renderer'ом, возвращает границы отрисовки
	/// </summary>
	public abstract Bounds Draw(Graphics graphics, RectangleF boundsRect, Orientation orientation, bool isLight);

	/// <summary>
	/// Применяет к объекту Graphics нужный поворот и возвращает границы отрисовки, развёрнутые нужным образом
	/// </summary>
	protected static Bounds ConfigureGraphics(Graphics graphics, RectangleF boundsRect, Orientation orientation)
	{
		graphics.SmoothingMode = SmoothingMode.AntiAlias;

		var graphicsBounds = BoundsFromRect(boundsRect);

		if (orientation == Orientation.Right)
		{
			return graphicsBounds;
		}

		var transform = graphics.Transform;
		transform.RotateAt((int)orientation, graphicsBounds.Center);
		graphics.Transform = transform;
		transform.Dispose();

		if (orientation == Orientation.Bottom || orientation == Orientation.Top)
			(graphicsBounds.Width, graphicsBounds.Height) = (graphicsBounds.Height, graphicsBounds.Width);

		return graphicsBounds;
	}

	/// <summary>
	/// Возвращает границы рисования преобразованные из RectangleF в Bounds
	/// </summary>
	private static Bounds BoundsFromRect(RectangleF boundsRect)
	{
		boundsRect.X = -0.5f;
		boundsRect.Y = -0.5f;

		var pivot = new PointF(0.5f, 0.5f);

		return Bounds.FromRectangle(boundsRect, pivot);
	}

	/// <summary>
	/// Отрисовывает рамку ошибки
	/// </summary>
	protected void DrawErrorRectangle(Graphics graphics, Bounds errorBounds, bool isLight)
	{
		if (isLight)
		{
			return;
		}

		var errorPoints = errorBounds.GetPoints(-ErrorLineWidth / 2f);
		using var errorPen = new Pen(Colors.Error, ErrorLineWidth);
		graphics.DrawClosedCurve(errorPen, errorPoints, 0, FillMode.Alternate);
	}

	/// <summary>
	/// Отрисовывает рамку предупреждения
	/// </summary>
	protected void DrawWarningRectangle(Graphics graphics, Bounds errorBounds)
	{
		var errorPoints = errorBounds.GetPoints(-ErrorLineWidth / 2f);
		using var errorPen = new Pen(Colors.Warning, ErrorLineWidth);
		graphics.DrawClosedCurve(errorPen, errorPoints, 0, FillMode.Alternate);
	}

	/// <summary>
	/// Отрисовывает прямоугольник блокировки
	/// </summary>
	protected static void DrawBlockRectangle(Graphics graphics, Bounds errorBoudns)
	{
		var blockRect = errorBoudns.ToRectangleF();
		using var brush = new SolidBrush(Colors.Blocked);
		graphics.FillRectangle(brush, blockRect);
	}
}
