using System.Drawing;
using System.Drawing.Drawing2D;

namespace NtoLib.Valves.Render
{
    internal class SlideGateRenderer : BaseRenderer
    {
        /// <summary>Толщина паза задвижки относительно ширины шибера</summary>
        private const float _relativeGrooveWidth = 0.12f;

        /// <summary>Толщина задвижки относительно внутренней ширины паза</summary>
        private const float _relativeGateWidth = 0.75f;



        public SlideGateRenderer(ValveControl valveControl) : base(valveControl)
        {
            LineWidth = 4f;

            ErrorLineWidth = 2f;
            ErrorOffset = 5f;
        }



        public override void Draw(Graphics graphics, PaintData paintData)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Status status = Control.Status;
            RectangleF valveRect = GetElementRect(paintData);
            DrawValve(graphics, valveRect, status, paintData.IsLight);
            DrawGroove(graphics, valveRect, status, paintData.IsLight);

            if(status.Error)
                DrawErrorRectangle(graphics, valveRect, paintData);
        }



        private void DrawValve(Graphics graphics, RectangleF valveRect, Status status, bool isLight)
        {
            Color[] colors = GetValveColors(status, isLight);
            PointF[][] valvePoints = GetValvePoints(valveRect);
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

        private void DrawGroove(Graphics graphics, RectangleF valveRect, Status status, bool isLight)
        {
            RectangleF grooveRect = GetGrooveRect(valveRect);
            PointF[] groovePoints = GetGroovePoints(grooveRect);

            using(Pen pen = new Pen(RenderParams.ColorLines, LineWidth))
                graphics.DrawClosedCurve(pen, groovePoints, 0, FillMode.Alternate);

            DrawGate(graphics, grooveRect, status, isLight);
        }

        private void DrawGate(Graphics graphics, RectangleF valveRect, Status status, bool isLigth)
        {
            RectangleF gateRect = GetGateRect(valveRect, status, isLigth);

            using(Brush brush = new SolidBrush(RenderParams.ColorLines))
                graphics.FillRectangle(brush, gateRect);
        }



        private RectangleF GetGrooveRect(RectangleF valveRect)
        {
            RectangleF grooveRect = new RectangleF();
            float grooveWidth = valveRect.Width * _relativeGrooveWidth;

            grooveRect.X = valveRect.X + valveRect.Width / 2f - grooveWidth / 2f;
            grooveRect.Y = valveRect.Y;
            grooveRect.Width = grooveWidth;
            grooveRect.Height = valveRect.Height * (2f / 3f);

            return grooveRect;
        }

        private PointF[] GetGroovePoints(RectangleF grooveRect)
        {
            float offset = LineWidth / 2f;

            float x0 = grooveRect.X + offset;
            float y0 = grooveRect.Y + offset;
            float x1 = grooveRect.X + grooveRect.Width - offset;
            float y1 = grooveRect.Y + grooveRect.Height - offset;

            PointF[] points = new PointF[] {
                new PointF(x0, y0),
                new PointF(x1, y0),
                new PointF(x1, y1),
                new PointF(x0, y1)
            };

            return points;
        }

        private RectangleF GetGateRect(RectangleF grooveRect, Status status, bool isLight)
        {
            RectangleF gateRect = new RectangleF();
            float gateWidth = (grooveRect.Width - 2f * LineWidth) * _relativeGateWidth;
            float gap = (grooveRect.Width - 2f * LineWidth - gateWidth) / 2f;

            gateRect.X = grooveRect.X + (grooveRect.Width - gateWidth) / 2f;
            gateRect.Width = gateWidth;

            float gateHeight = (grooveRect.Height - 2f * LineWidth - 3f * gap) / 2f;
            gateRect.Height = gateHeight;

            float offset = (grooveRect.Height - gateHeight * 2f) / 3f;
            if(status.State == State.Opened)
            {
                gateRect.Y = grooveRect.Y + offset;
            }
            else if(status.State == State.Closed)
            {
                gateRect.Y = grooveRect.Y + offset * 2f + gateHeight;
            }
            else if(status.State == State.Opening || status.State == State.Closing)
            {
                if(isLight)
                {
                    gateRect.Y = grooveRect.Y + offset;
                }
                else
                {
                    gateRect.Y = grooveRect.Y + offset * 2f + gateHeight;
                }
            }
            else
            {
                gateRect.Y = grooveRect.Y + offset * 1.5f + gateHeight * 0.5f;
            }

            return gateRect;
        }

        private PointF[][] GetValvePoints(RectangleF valveRect)
        {
            float x0 = valveRect.X + LineWidth * 0.5f;
            float y0 = valveRect.Y + LineWidth * 0.5f;
            float x1 = valveRect.X + valveRect.Width - LineWidth * 0.577f;
            float y1 = valveRect.Y + valveRect.Height - LineWidth * 0.577f;

            float grooveWidth = valveRect.Width * _relativeGrooveWidth;
            float xCl = (x0 + x1) / 2f - grooveWidth / 2f;
            float xCr = xCl + grooveWidth;
            float yC = (y0 + y1) / 2f;

            PointF[][] points = new PointF[2][];
            points[0] = new[] {
                    new PointF(x0, y0),
                    new PointF(xCl, yC),
                    new PointF(x0, y1)
            };
            points[1] = new[] {
                    new PointF(x1, y1),
                    new PointF(xCr, yC),
                    new PointF(x1, y0)
            };

            return points;
        }
    }
}
