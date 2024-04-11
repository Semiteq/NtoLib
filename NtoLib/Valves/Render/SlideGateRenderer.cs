using NtoLib.Render;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NtoLib.Valves.Render
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
            ErrorOffset = 5f;
        }



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
            DrawValve(graphics, valveBounds, status, paintData.IsLight);
            DrawGrooveAndGate(graphics, valveBounds, status, paintData.IsLight);

            if(status.Error)
                DrawErrorRectangle(graphics, paintData.Bounds);
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

            using(Pen pen = new Pen(GetLineColor(Control.Status), LineWidth))
            {
                graphics.DrawClosedCurve(pen, valvePoints[0], 0, FillMode.Alternate);
                graphics.DrawClosedCurve(pen, valvePoints[1], 0, FillMode.Alternate);
            }
        }

        /// <summary>
        /// Отрисовывает паз для задвижки в виде прямоугольной рамки
        /// </summary>
        private void DrawGrooveAndGate(Graphics graphics, Bounds valveBounds, Status status, bool isLight)
        {
            Bounds grooveBounds = GetGrooveBounds(valveBounds);
            PointF[] groovePoints = grooveBounds.GetPoints(-LineWidth / 2f);

            using(Pen pen = new Pen(RenderParams.ColorLines, LineWidth))
                graphics.DrawClosedCurve(pen, groovePoints, 0, FillMode.Alternate);

            Bounds gateBounds = GetGateBounds(grooveBounds, status, isLight);

            using(Brush brush = new SolidBrush(RenderParams.ColorLines))
                graphics.FillRectangle(brush, gateBounds.ToRectangleF());
        }



        /// <summary>
        /// Возвращает массивы точек для двух треугольников шибера соответственно
        /// </summary>
        private PointF[][] GetValvePoints(Bounds valveBounds)
        {
            valveBounds.Width -= LineWidth;
            valveBounds.Height -= LineWidth * 1.154f;

            float offsetFromCenter = valveBounds.Width * _relativeGrooveWidth / 2f;
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
        private Bounds GetGrooveBounds(Bounds valveBounds)
        {
            Bounds grooveBounds = valveBounds;

            grooveBounds.Y -= valveBounds.Height / 6f;
            grooveBounds.Height = valveBounds.Height * 2f / 3f;
            grooveBounds.Width = valveBounds.Width * _relativeGrooveWidth;

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

            if(status.State == State.Opened || (status.State == State.Opening && isLight) || (status.State == State.Closing && !isLight))
            {
                gateBounds.Y -= grooveBounds.Height / 4f - LineWidth / 2f;
            }
            else if(status.State == State.Closed || (status.State == State.Opening && !isLight) || (status.State == State.Closing && isLight))
            {
                gateBounds.Y += grooveBounds.Height / 4f - LineWidth / 2f;
            }

            return gateBounds;
        }
    }
}
