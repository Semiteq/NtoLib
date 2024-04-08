using System.Drawing;
using System.Drawing.Drawing2D;

namespace NtoLib.Valves.Render
{
    internal class SmoothValveRenderer : CommonValveRenderer
    {
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

        private RectangleF GetCircleRect(RectangleF valveRect)
        {
            RectangleF circleRect = new RectangleF();

            circleRect.Width = valveRect.Height / 4f;
            circleRect.Height = circleRect.Width;

            circleRect.X = valveRect.X + valveRect.Width / 2f - circleRect.Width / 2f;
            circleRect.Y = valveRect.Y;

            return circleRect;
        }

        private PointF[] GetCircleLegPoints(RectangleF valveRect)
        {
            float x0 = valveRect.X + valveRect.Width / 2f;
            float y0 = valveRect.Y + valveRect.Height / 2f;
            float x1 = x0;
            float y1 = y0 - valveRect.Height / 4f;

            PointF[] points = new PointF[] {
                new PointF(x0, y0),
                new PointF(x1, y1)
            };

            return points;
        }

        private Color GetCircleColor(Status status, bool isLight)
        {
            Color color;

            if(status.State == State.NoData)
            {
                color = NDColor;
            }
            else if(status.State == State.Opened || status.State == State.SmothlyOpened)
            {
                color = OpenColor;
            }
            else if(status.State == State.Closed)
            {
                color = ClosedColor;
            }
            else
            {
                if(isLight)
                {
                    color = OpenColor;
                }
                else
                {
                    color = ClosedColor;
                }
            }

            return color;
        }
    }
}
