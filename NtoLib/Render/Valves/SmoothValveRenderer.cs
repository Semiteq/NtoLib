using NtoLib.Devices.Valves;
using System.Drawing;

namespace NtoLib.Render.Valves
{
    internal class SmoothValveRenderer : CommonValveRenderer
    {
        /// <summary>Диаметр кружочка относительно высоты клапана</summary>
        private const float _realitiveCircleDiameter = 0.3f;



        public SmoothValveRenderer(ValveControl valveControl) : base(valveControl) { }



        public override Bounds Draw(Graphics graphics, RectangleF boundsRect, Orientation orientation, bool isLight)
        {
            Bounds graphicsBounds = base.Draw(graphics, boundsRect, orientation, isLight);

            Status status = _control.Status;
            Bounds errorBounds = GetErrorBounds(graphicsBounds);
            Bounds valveBounds = GetValveBounds(errorBounds);

            DrawSmoothValveCirlce(graphics, valveBounds, status);

            return graphicsBounds;
        }



        /// <summary>
        /// Отрисовывает кружочек (и ножку) клапана плавной откачки
        /// </summary>
        private void DrawSmoothValveCirlce(Graphics graphics, Bounds valveBounds, Status status)
        {
            Bounds circleBounds = GetCircleBounds(valveBounds);
            RectangleF circleRect = circleBounds.ToRectangleF();
            using(Brush brush = new SolidBrush(GetCircleColor(status)))
                graphics.FillEllipse(brush, circleRect);

            using(Pen pen = new Pen(Colors.Lines, LineWidth))
            {
                graphics.DrawEllipse(pen, circleRect);

                PointF[] legPoints = GetCircleLegPoints(valveBounds, circleBounds);
                graphics.DrawLines(pen, legPoints);
            }
        }

        /// <summary>
        /// Возвращает границы, в которых должен быть отрисован кружочек клапана плавной откачки
        /// </summary>
        private Bounds GetCircleBounds(Bounds valveBounds)
        {
            Bounds circleBounds = valveBounds;

            float circleDiameter = valveBounds.Height * _realitiveCircleDiameter;
            circleBounds.Y -= (valveBounds.Height - circleDiameter) / 2f;
            circleBounds.Width = circleDiameter;
            circleBounds.Height = circleDiameter;

            return circleBounds;
        }

        /// <summary>
        /// Вощвращает точки для отрисочки по ним ножки кружочка клапана плавной откачки
        /// </summary>
        private PointF[] GetCircleLegPoints(Bounds valveBounds, Bounds circelBounds)
        {
            return new PointF[] {
                valveBounds.Center,
                circelBounds.CenterBottom
            };
        }

        /// <summary>
        /// Возвращает цвет кружочка клапана плавной откачки в зависимости от состояния
        /// </summary>
        private Color GetCircleColor(Status status)
        {
            Color color;

            if(!status.ConnectionOk)
            {
                color = Colors.NoData;
            }
            else if(status.Opened || status.OpenedSmoothly)
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
}
