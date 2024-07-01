using NtoLib.Valves;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NtoLib.Render.Valves
{
    internal class CommonValveRenderer : ValveBaseRenderer
    {
        public CommonValveRenderer(ValveControl valveControl) : base(valveControl)
        {
            LineWidth = 2f;

            ErrorLineWidth = 2f;
            ErrorOffset = 2f;
        }



        public override Bounds Draw(Graphics graphics, RectangleF boundsRect, Orientation orientation, bool isLight)
        {
            Bounds graphicsBounds = ConfigurateGraphics(graphics, boundsRect, orientation);

            Status status = _control.Status;
            Bounds errorBounds = GetErrorBounds(graphicsBounds);
            Bounds valveBounds = GetValveBounds(errorBounds);

            if(IsBlocked(status))
                DrawBlockRectangle(graphics, errorBounds);

            DrawValve(graphics, valveBounds, status, isLight);

            if(status.AnyError)
                DrawErrorRectangle(graphics, errorBounds);

            return graphicsBounds;
        }



        /// <summary>
        /// Отрисовывает клапан, состоящий из двух треугольников в заданной области
        /// </summary>
        protected void DrawValve(Graphics graphics, Bounds valveBounds, Status status, bool isLight)
        {
            Color[] colors = GetValveColors(status, isLight);
            PointF[][] valvePoints = GetValvePoints(valveBounds);
            for(int i = 0; i < valvePoints.Length; i++)
            {
                using(SolidBrush brush = new SolidBrush(colors[i]))
                    graphics.FillClosedCurve(brush, valvePoints[i], FillMode.Alternate, 0);

            }

            using(Pen pen = new Pen(Colors.Lines, LineWidth))
            {
                graphics.DrawClosedCurve(pen, valvePoints[0], 0, FillMode.Alternate);
                graphics.DrawClosedCurve(pen, valvePoints[1], 0, FillMode.Alternate);
            }
        }

        /// <summary>
        /// Возвращет границы, в которых должна быть отрисована рамка ошибки
        /// или прямоугольник блокировки
        /// </summary>
        protected Bounds GetErrorBounds(Bounds graphicsBounds)
        {
            Bounds errorBounds = graphicsBounds;
            //errorBounds.Height *= 0.8f;
            return errorBounds;
        }

        /// <summary>
        /// Возвращает границы, в которых должен быть отрисован клапан/шибер
        /// </summary>
        protected Bounds GetValveBounds(Bounds errorBounds)
        {
            Bounds valveBounds = errorBounds;

            float offset = 2f * (ErrorLineWidth + ErrorOffset);
            valveBounds.Width -= offset;
            valveBounds.Height -= offset;

            return valveBounds;
        }

        /// <summary>
        /// Возвращает массивы точек для двух треугольников клапана соответственно
        /// </summary>
        protected PointF[][] GetValvePoints(Bounds valveBounds)
        {
            valveBounds.Width -= LineWidth;
            valveBounds.Height -= LineWidth * 1.154f;

            PointF[][] points = new PointF[2][];
            points[0] = new[] {
                    valveBounds.LeftTop,
                    valveBounds.Center,
                    valveBounds.LeftBottom
            };
            points[1] = new[] {
                    valveBounds.RightTop,
                    valveBounds.Center,
                    valveBounds.RightBottom
            };

            return points;
        }
    }
}
