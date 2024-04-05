using System.Drawing;

namespace NtoLib.Valves.Render
{
    internal abstract class BaseRenderer
    {
        protected readonly Color LineColor = Color.Black;
        protected readonly Color BlockedColor = Color.Orange;
        protected readonly Color OpenColor = Color.LimeGreen;
        protected readonly Color ClosedColor = Color.White;
        protected readonly Color NDColor = Color.Gainsboro;
        protected readonly Color ErrorColor = Color.Red;



        public abstract void Paint(Graphics graphics, PaintData data, State state);



        protected PointF[] GetErrorRectPoints(RectangleF valveRect, float errorLineWidth, float errorOffset)
        {
            float offset = 0.5f * errorLineWidth + errorOffset;
            float x0 = valveRect.X - offset;
            float y0 = valveRect.Y - offset;
            float x1 = valveRect.X + valveRect.Width + offset;
            float y1 = valveRect.Y + valveRect.Height + offset;

            PointF[] points = new PointF[6];
            points[0] = new PointF(x0, y0);
            points[1] = new PointF(x0, y1);
            points[2] = new PointF(x1, y1);
            points[3] = new PointF(x1, y0);
            points[4] = new PointF(x0, y0);
            points[5] = new PointF(x0, y1);
            return points;
        }

        protected PointF[] GetDebugBoundPoints(RectangleF Bounds)
        {
            PointF[] points = new PointF[6];
            points[0] = new PointF(Bounds.Left, Bounds.Top);
            points[1] = new PointF(Bounds.Left, Bounds.Bottom);
            points[2] = new PointF(Bounds.Right, Bounds.Bottom);
            points[3] = new PointF(Bounds.Right, Bounds.Top);
            points[4] = new PointF(Bounds.Left, Bounds.Top);
            points[5] = new PointF(Bounds.Left, Bounds.Bottom);
            return points;
        }
    }
}
