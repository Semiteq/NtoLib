using System.Drawing;
using System.Drawing.Drawing2D;

using NtoLib.Devices.Pumps;

namespace NtoLib.Render.Pumps
{
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
			Bounds graphicsBounds = ConfigurateGraphics(graphics, boundsRect, orientation);

			Status status = _control.Status;
			Bounds errorBounds = graphicsBounds;
			Bounds pumpBounds = GetPumpBounds(errorBounds);
			RectangleF pumpRect = pumpBounds.ToRectangleF();
			Bounds triangleBounds = GetMainTriangleBounds(pumpBounds);

			if (IsBlocked(status))
				DrawBlockRectangle(graphics, errorBounds);

			Color pumpColor = GetCirlceColor(status);
			Color[] triangleColors = GetPumpTriangleColors(status, isLight);
			PointF[][] points = GetPumpTrianglePoints(triangleBounds);

			using (SolidBrush brush = new SolidBrush(pumpColor))
				graphics.FillEllipse(brush, pumpRect);
			using (SolidBrush brush = new SolidBrush(triangleColors[0]))
				graphics.FillClosedCurve(brush, points[0], FillMode.Alternate, 0);
			using (SolidBrush brush = new SolidBrush(triangleColors[1]))
				graphics.FillClosedCurve(brush, points[1], FillMode.Alternate, 0);


			PointF[] trianglePoints = GetMainTrianglePoints(triangleBounds);
			using (Pen pen = new Pen(Colors.Lines, LineWidth))
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
		protected Bounds GetPumpBounds(Bounds errorBounds)
		{
			Bounds valveBounds = errorBounds;

			float offset = 2f * (ErrorLineWidth + ErrorOffset);
			valveBounds.Width -= offset;
			valveBounds.Height -= offset;

			return valveBounds;
		}

		/// <summary>
		/// Возвращает границы треугольника
		/// </summary>
		protected Bounds GetMainTriangleBounds(Bounds pumpBounds)
		{
			Bounds triangleBounds = pumpBounds;

			triangleBounds.Width = triangleBounds.Width / 2f - LineWidth;
			triangleBounds.Height -= 2 * LineWidth;
			triangleBounds.X += triangleBounds.Width / 2f;

			return triangleBounds;
		}

		/// <summary>
		/// Возвращает массив точек треугольника насоса
		/// </summary>
		protected PointF[] GetMainTrianglePoints(Bounds mainTiangleBounds)
		{
			return new PointF[] {
				mainTiangleBounds.LeftTop,
				mainTiangleBounds.RightCenter,
				mainTiangleBounds.LeftBottom
			};
		}

		/// <summary>
		/// Возвращает массивы точек для закрашивания двух половинов треугольника
		/// </summary>
		protected PointF[][] GetPumpTrianglePoints(Bounds mainTiangleBounds)
		{
			PointF[][] points = new PointF[2][];
			points[0] = new[] {
				mainTiangleBounds.LeftTop,
				mainTiangleBounds.RightCenter,
				mainTiangleBounds.LeftCenter
			};
			points[1] = new[] {
				mainTiangleBounds.LeftBottom,
				mainTiangleBounds.RightCenter,
				mainTiangleBounds.LeftCenter
			};

			return points;
		}

		/// <summary>
		/// Возвращает цвета первой и второй половины треугольника в зависимости от статуса
		/// </summary>
		protected Color[] GetPumpTriangleColors(Status status, bool isLight)
		{
			Color[] colors = new Color[2];

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

		protected Color GetCirlceColor(Status status)
		{
			if (!status.Use || !status.ConnectionOk)
				return Colors.NoData;
			else
				return Colors.Closed;
		}

		/// <summary>
		/// Возвращет истину, если должна быть показана блокировка
		/// </summary>
		protected bool IsBlocked(Status status)
		{
			if ((status.Stopped || status.Decelerating) && (status.BlockStart || status.ForceStop))
				return true;
			if ((status.WorkOnNominalSpeed || status.Accelerating) && status.BlockStop)
				return true;

			return false;
		}
	}
}
