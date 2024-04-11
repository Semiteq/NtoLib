using NtoLib.Render;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NtoLib.Valves.Render
{
    internal class SmoothValveRenderer : CommonValveRenderer
    {
        /// <summary>Диаметр кружочка относительно высоты клапана</summary>
        private const float _realitiveCircleDiameter = 0.33f;



        public SmoothValveRenderer(ValveControl valveControl) : base(valveControl) { }



        public override void Draw(Graphics graphics, PaintData paintData)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if(paintData.Orientation == Orientation.Vertical)
            {
                Matrix transform = graphics.Transform;
                transform.RotateAt(90f, paintData.Bounds.Center);
                graphics.Transform = transform;
                transform.Dispose();

                (paintData.Bounds.Width, paintData.Bounds.Height) = (paintData.Bounds.Height, paintData.Bounds.Width);
            }

            Status status = Control.Status;
            Bounds valveBounds = GetValveBounds(paintData);
            DrawValve(graphics, valveBounds, paintData, status);
            DrawSmoothValveCirlce(graphics, valveBounds, paintData, status);

            if(status.Error)
                DrawErrorRectangle(graphics, paintData.Bounds);
        }



        /// <summary>
        /// Отрисовывает кружочек (и ножку) клапана плавной откачки
        /// </summary>
        private void DrawSmoothValveCirlce(Graphics graphics, Bounds valveBounds, PaintData paintData, Status status)
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
                color = RenderParams.ColorNoData;
            }
            else if(status.State == State.Closed)
            {
                color = RenderParams.ColorClosed;
            }
            else
            {
                color = RenderParams.ColorOpened;
            }

            return color;
        }
    }
}
