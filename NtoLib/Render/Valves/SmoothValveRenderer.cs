using NtoLib.Valves;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NtoLib.Render.Valves
{
    internal class SmoothValveRenderer : CommonValveRenderer
    {
        /// <summary>Диаметр кружочка относительно высоты клапана</summary>
        private const float _realitiveCircleDiameter = 0.3f;



        public SmoothValveRenderer(ValveControl valveControl) : base(valveControl) { }



        public override void Draw(Graphics graphics, RectangleF boundsRect, Orientation orientation, bool isLight)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Bounds graphicsBounds = BoundsFromRect(boundsRect);

            if(orientation == Orientation.Vertical)
            {
                Matrix transform = graphics.Transform;
                transform.RotateAt(90f, graphicsBounds.Center);
                graphics.Transform = transform;
                transform.Dispose();

                (graphicsBounds.Width, graphicsBounds.Height) = (graphicsBounds.Height, graphicsBounds.Width);
            }

            Status status = Control.Status;
            Bounds valveBounds = GetValveBounds(graphicsBounds);
            DrawValve(graphics, valveBounds, status, isLight);
            DrawSmoothValveCirlce(graphics, valveBounds, status);

            if(status.Error)
                DrawErrorRectangle(graphics, graphicsBounds);
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

            using(Pen pen = new Pen(GetLineColor(status), LineWidth))
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

            if(status.State == State.NoData)
            {
                color = Colors.NoData;
            }
            else if(status.State == State.Closed)
            {
                color = Colors.Closed;
            }
            else
            {
                color = Colors.Opened;
            }

            return color;
        }
    }
}
