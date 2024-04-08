using System.Drawing;
using System.Drawing.Drawing2D;

namespace NtoLib.Valves.Render
{
    internal class SmoothValveRenderer : CommonValveRenderer
    {
        /// <summary>Диаметр кружочка относительно высоты клапана</summary>
        private const float _circleRealitiveDiameter = 0.33f;



        public SmoothValveRenderer(ValveControl valveControl) : base(valveControl)
        {

        }



        public override void Draw(Graphics graphics, PaintData paintData)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF valveRect = GetElementRect(paintData);
            DrawValve(graphics, valveRect, paintData, ValveControl.Status);
            DrawSmoothValveCirlce(graphics, valveRect, paintData, ValveControl.Status);
        }



        /// <summary>
        /// Отрисовывает кружочек (и его ножку) клапана плавной откачки
        /// </summary>
        private void DrawSmoothValveCirlce(Graphics graphics, RectangleF valveRect, PaintData paintData, Status status)
        {
            RectangleF circleRect = GetCircleRect(valveRect);
            using(Brush brush = new SolidBrush(GetCircleColor(status, paintData.IsLight)))
                graphics.FillEllipse(brush, circleRect);

            using(Pen pen = new Pen(GetLineColor(status)))
            {
                graphics.DrawEllipse(pen, circleRect);

                PointF[] legPoints = GetCircleLegPoints(valveRect);
                graphics.DrawLines(pen, legPoints);
            }
        }

        /// <summary>
        /// Возвращает границы, в которых должен быть отрисован кружочек клапана плавной откачки
        /// </summary>
        private RectangleF GetCircleRect(RectangleF valveRect)
        {
            RectangleF circleRect = new RectangleF();

            float circleDiameter = valveRect.Height * _circleRealitiveDiameter;
            circleRect.Width = circleDiameter;
            circleRect.Height = circleRect.Width;

            circleRect.X = valveRect.X + valveRect.Width / 2f - circleDiameter / 2f;
            circleRect.Y = valveRect.Y;

            return circleRect;
        }

        /// <summary>
        /// Вощвращает точки для отрисочки по ним кружочка клапана плавной откачки
        /// </summary>
        private PointF[] GetCircleLegPoints(RectangleF valveRect)
        {
            float x0 = valveRect.X + valveRect.Width / 2f;
            float y0 = valveRect.Y + valveRect.Height / 2f;
            float x1 = x0;
            float y1 = valveRect.Y + valveRect.Height * _circleRealitiveDiameter;

            PointF[] points = new PointF[] {
                new PointF(x0, y0),
                new PointF(x1, y1)
            };

            return points;
        }

        /// <summary>
        /// Возвращает цвет кружочка клапана плавной откачки в зависимости от состояния
        /// </summary>
        private Color GetCircleColor(Status status, bool isLight)
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
