using System.Drawing;
using System.Drawing.Drawing2D;

namespace NtoLib.Valves.Render
{
    internal class CommonValveRenderer : BaseRenderer
    {
        public CommonValveRenderer(ValveControl valveControl) : base(valveControl)
        {

        }



        public override void Draw(Graphics graphics, PaintData paintData)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF valveRect = GetElementRect(paintData);
            DrawValve(graphics, valveRect, paintData, ValveControl.Status);
        }



        /// <summary>
        /// Отрисовывает клапан, состоящий из двух треугольников в заданной области
        /// </summary>
        protected void DrawValve(Graphics graphics, RectangleF valveRect, PaintData paintData, Status status)
        {
            Color[] colors = GetValveColors(status, paintData.IsLight);
            PointF[][] valvePoints = GetValvePoints(valveRect, paintData.Orientation, paintData.LineWidth);
            for(int i = 0; i < valvePoints.Length; i++)
            {
                using(SolidBrush brush = new SolidBrush(colors[i]))
                    graphics.FillClosedCurve(brush, valvePoints[i], FillMode.Alternate, 0);

            }

            using(Pen pen = new Pen(GetLineColor(ValveControl.Status)))
            {
                graphics.DrawClosedCurve(pen, valvePoints[0], 0, FillMode.Alternate);
                graphics.DrawClosedCurve(pen, valvePoints[1], 0, FillMode.Alternate);
            }

            if(status.Error)
            {
                PointF[] errorPoints = GetErrorRectPoints(valveRect, paintData.ErrorLineWidth, paintData.ErrorOffset);
                using(Pen errorPen = new Pen(RenderParams.ColorError))
                    graphics.DrawLines(errorPen, errorPoints);
            }
        }

        /// <summary>
        /// Возвращает массивы точек для двух треугольников клапана соответственно
        /// </summary>
        protected PointF[][] GetValvePoints(RectangleF valveRect, Orientation orientation, float lineWidth)
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
        /// Возвращает цвет обводки клапана в зависимости от его статуса
        /// </summary>
        protected Color GetLineColor(Status status)
        {
            State state = status.State;
            if((state == State.Opened && status.BlockClosing) || (state == State.Closed && status.BlockOpening))
                return RenderParams.ColorBlock;
            else
                return RenderParams.ColorLines;
        }
    }
}
