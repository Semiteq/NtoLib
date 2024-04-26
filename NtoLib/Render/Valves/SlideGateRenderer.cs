using NtoLib.Valves;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NtoLib.Render.Valves
{
    internal class SlideGateRenderer : ValveBaseRenderer
    {
        /// <summary>Толщина паза задвижки относительно ширины шибера</summary>
        private const float _relativeGrooveWidth = 0.12f;

        /// <summary>Толщина задвижки относительно внутренней ширины паза</summary>
        private const float _relativeGateWidth = 0.75f;



        public SlideGateRenderer(ValveControl valveControl) : base(valveControl)
        {
            LineWidth = 2f;

            ErrorLineWidth = 2f;
            ErrorOffset = 2f;
        }



        public override Bounds Draw(Graphics graphics, RectangleF boundsRect, Orientation orientation, bool isLight)
        {
            Bounds graphicsBounds = ConfigurateGraphics(graphics, boundsRect, orientation);

            Status status = _control.Status;
            Bounds errorBounds = graphicsBounds;
            Bounds valveBounds = GetValveBounds(errorBounds);

            if(IsBlocked(status))
                DrawBlockRectangle(graphics, errorBounds);

            DrawValve(graphics, valveBounds, status, isLight);
            DrawGrooveAndGate(graphics, errorBounds, status, isLight);

            if(status.AnyError)
                DrawErrorRectangle(graphics, errorBounds);

            return graphicsBounds;
        }



        /// <summary>
        /// Отрисовывает клапан, состоящий из двух треугольников в заданной области, 
        /// при этом оставляет между треугольниками зазор для задвижки шибера
        /// </summary>
        private void DrawValve(Graphics graphics, Bounds valveBounds, Status status, bool isLight)
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
        /// Отрисовывает паз для задвижки в виде прямоугольной рамки
        /// </summary>
        private void DrawGrooveAndGate(Graphics graphics, Bounds errorBounds, Status status, bool isLight)
        {
            Bounds grooveBounds = GetGrooveBounds(errorBounds);
            PointF[] groovePoints = grooveBounds.GetPoints(-LineWidth / 2f);

            using(Pen pen = new Pen(Colors.Lines, LineWidth))
                graphics.DrawClosedCurve(pen, groovePoints, 0, FillMode.Alternate);

            Bounds gateBounds = GetGateBounds(grooveBounds, status, isLight);

            using(Brush brush = new SolidBrush(Colors.Lines))
                graphics.FillRectangle(brush, gateBounds.ToRectangleF());
        }



        /// <summary>
        /// Возвращает границы, в которых должен быть отрисован клапан/шибер
        /// </summary>
        private Bounds GetValveBounds(Bounds errorBounds)
        {
            Bounds valveBounds = errorBounds;

            float offset = 2f * (ErrorLineWidth + ErrorOffset);
            valveBounds.Width -= offset;
            valveBounds.Height -= offset;
            valveBounds.Height *= 0.8f;

            return valveBounds;
        }

        /// <summary>
        /// Возвращает массивы точек для двух треугольников шибера соответственно
        /// </summary>
        private PointF[][] GetValvePoints(Bounds valveBounds)
        {
            valveBounds.Width -= LineWidth;
            valveBounds.Height -= LineWidth * 1.154f;

            float offsetFromCenter = valveBounds.Width * _relativeGrooveWidth / 2f + LineWidth / 2f;
            PointF leftTriangleCenter = new PointF(valveBounds.CenterX - offsetFromCenter, valveBounds.CenterY);
            PointF rightTriangleCenter = new PointF(valveBounds.CenterX + offsetFromCenter, valveBounds.CenterY);

            PointF[][] points = new PointF[2][];
            points[0] = new[] {
                    valveBounds.LeftTop,
                    leftTriangleCenter,
                    valveBounds.LeftBottom
            };
            points[1] = new[] {
                    valveBounds.RightTop,
                    rightTriangleCenter,
                    valveBounds.RightBottom
            };

            return points;
        }

        /// <summary>
        /// Возвращает границы паза для задвижки шибера
        /// </summary>
        private Bounds GetGrooveBounds(Bounds errorBounds)
        {
            Bounds grooveBounds = errorBounds;


            float offset = 2f * (ErrorLineWidth + ErrorOffset);
            grooveBounds.Height = (errorBounds.Height - offset) * (2f / 3f);
            grooveBounds.Width = errorBounds.Width * _relativeGrooveWidth;
            grooveBounds.Y -= grooveBounds.Height / 4f;

            return grooveBounds;
        }

        /// <summary>
        /// Возвращает границы задвижки шибера
        /// </summary>
        private Bounds GetGateBounds(Bounds grooveBounds, Status status, bool isLight)
        {
            Bounds gateBounds = grooveBounds;

            gateBounds.Width = (grooveBounds.Width - 2f * LineWidth) * _relativeGateWidth;

            float gap = (grooveBounds.Width - 2f * LineWidth - gateBounds.Width) / 2f;
            gateBounds.Height = (grooveBounds.Height - 2f * LineWidth - 3f * gap) / 2f;

            if(status.State == State.Opened || (status.State == State.OpeningClosing && isLight))
            {
                gateBounds.Y -= grooveBounds.Height / 4f - LineWidth / 2f;
            }
            else if(status.State == State.Closed || (status.State == State.OpeningClosing && !isLight))
            {
                gateBounds.Y += grooveBounds.Height / 4f - LineWidth / 2f;
            }

            return gateBounds;
        }
    }
}
