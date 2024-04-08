using System.Drawing;
using System.Drawing.Drawing2D;

namespace NtoLib.Valves.Render
{
    internal class CommonValveRenderer : BaseRenderer
    {
        public CommonValveRenderer(ValveControl valveControl) : base(valveControl)
        {

        }



        public override void Paint(Graphics graphics, PaintData paintData)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF valveRect = GetElementRect(paintData);
            PointF[][] valvePoints = GetValvePoints(valveRect, paintData.Orientation, paintData.LineWidth);


            Status status = ValveControl.Status;

            Color[] colors = DefineValveColors(status, paintData.IsLight);
            for(int i = 0; i < valvePoints.Length; i++)
            {
                using(SolidBrush brush = new SolidBrush(colors[i]))
                    graphics.FillClosedCurve(brush, valvePoints[i], FillMode.Alternate, 0);

            }

            using(Pen pen = new Pen(DefineLineColor(ValveControl.Status)))
            {
                graphics.DrawClosedCurve(pen, valvePoints[0], 0, FillMode.Alternate);
                graphics.DrawClosedCurve(pen, valvePoints[1], 0, FillMode.Alternate);
            }

            if(status.Error)
            {
                PointF[] errorPoints = GetErrorRectPoints(valveRect, paintData.ErrorLineWidth, paintData.ErrorOffset);
                using(Pen errorPen = new Pen(ErrorColor))
                    graphics.DrawLines(errorPen, errorPoints);
            }
        }



        /// <summary>
        /// Возвращает массивы точек для двух треугольников соответственно
        /// </summary>
        private PointF[][] GetValvePoints(RectangleF valveRect, Orientation orientation, float lineWidth)
        {
            float x0 = valveRect.X + lineWidth * 0.5f;
            float y0 = valveRect.Y + lineWidth * 0.5f;
            float x1 = valveRect.X + valveRect.Width - lineWidth * 0.577f;
            float y1 = valveRect.Y + valveRect.Height - lineWidth * 0.577f;
            float xC = (x0 + x1) / 2f;
            float yC = (y0 + y1) / 2f;

            PointF[][] points = new PointF[2][];
            points[0] = new[] {
                    new PointF(x0, y0),
                    new PointF(xC, yC),
                    new PointF(x0, y1)
            };
            points[1] = new[] {
                    new PointF(x1, y1),
                    new PointF(xC, yC),
                    new PointF(x1, y0)
            };

            if(orientation == Orientation.Vertical)
            {
                points[0][2] = new PointF(x1, y0);
                points[1][2] = new PointF(x0, y1);
            }

            return points;
        }

        /// <summary>
        /// Возвращает цвета первого и второго треугольника соответственно
        /// </summary>
        private Color[] DefineValveColors(Status status, bool isLight)
        {
            Color[] colors = new Color[2];

            if(status.State == State.NoData)
            {
                colors[0] = NDColor;
                colors[1] = NDColor;
            }
            else if(status.State == State.Open)
            {
                colors[0] = OpenColor;
                colors[1] = OpenColor;
            }
            else if(status.State == State.Closed)
            {
                colors[0] = ClosedColor;
                colors[1] = ClosedColor;
            }
            else
            {
                if(isLight)
                {
                    colors[0] = OpenColor;
                    colors[1] = ClosedColor;
                }
                else
                {
                    colors[0] = ClosedColor;
                    colors[1] = OpenColor;
                }
            }

            return colors;
        }

        /// <summary>
        /// Возвращает цвет обводки клапана
        /// </summary>
        private Color DefineLineColor(Status status)
        {
            State state = status.State;
            if((state == State.Open && status.BlockClosing) || (state == State.Closed && status.BlockOpening))
                return BlockedColor;
            else
                return LineColor;
        }
    }
}
