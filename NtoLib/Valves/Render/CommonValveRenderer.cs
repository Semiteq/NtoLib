using System.Drawing;
using System.Drawing.Drawing2D;

namespace NtoLib.Valves.Render
{
    internal class CommonValveRenderer : BaseRenderer
    {
        private const float _widthToHeightRatio = 1.732f;



        public override void Paint(Graphics graphics, PaintData data, State state)
        {
            RectangleF valveRect = GetValveRect(data, _widthToHeightRatio);
            PointF[] valvePoints = GetValvePoints(valveRect, data.Orientation, data.LineWidth);

            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color lineColor;
            if(state.Opened && state.BlockClosing || state.Closed && state.BlockOpening)
                lineColor = BlockedColor;
            else
                lineColor = LineColor;
            using(Pen linePen = new Pen(lineColor, data.LineWidth))
                graphics.DrawClosedCurve(linePen, valvePoints, 0, FillMode.Alternate);

            Color valveColor;
            if(state.Opened)
                valveColor = OpenColor;
            else if(state.Closed)
                valveColor = ClosedColor;
            else if(!state.ConnectionOk)
                valveColor = NDColor;
            else
                valveColor = Color.Fuchsia;
            using(SolidBrush valveBrush = new SolidBrush(valveColor))
                graphics.FillClosedCurve(valveBrush, valvePoints, FillMode.Alternate, 0);

            if(state.Error)
            {
                PointF[] errorPoints = GetErrorRectPoints(valveRect, data.ErrorLineWidth, data.ErrorOffset);
                using(Pen errorPen = new Pen(ErrorColor, data.ErrorLineWidth))
                    graphics.DrawLines(errorPen, errorPoints);
            }
        }



        private RectangleF GetValveRect(PaintData data, float widthToHeightRatio)
        {
            RectangleF clampedBounds = data.Bounds;
            clampedBounds.Width -= 2f * (data.ErrorLineWidth + data.ErrorOffset);
            clampedBounds.Height -= 2f * (data.ErrorLineWidth + data.ErrorOffset);

            if(data.Shape == Shape.Right)
            {
                if(data.Orientation == Orientation.Vertical)
                    widthToHeightRatio = 1 / widthToHeightRatio;

                float ratio = clampedBounds.Width / clampedBounds.Height;
                if(ratio > widthToHeightRatio)
                    clampedBounds.Width = clampedBounds.Height * widthToHeightRatio;
                else if(ratio < widthToHeightRatio)
                    clampedBounds.Height = clampedBounds.Width / widthToHeightRatio;
            }

            clampedBounds.X = (data.Bounds.Width - clampedBounds.Width) / 2f;
            clampedBounds.Y = (data.Bounds.Height - clampedBounds.Height) / 2f;
            return clampedBounds;
        }

        private PointF[] GetValvePoints(RectangleF valveRect, Orientation orientation, float lineWidth)
        {
            float x0 = valveRect.X + lineWidth * 0.5f;
            float y0 = valveRect.Y + lineWidth * 0.5f;
            float x1 = valveRect.X + valveRect.Width - lineWidth * 0.577f;
            float y1 = valveRect.Y + valveRect.Height - lineWidth * 0.577f;


            PointF[] points;
            if(orientation == Orientation.Horizontal)
            {
                points = new[]
                {
                    new PointF(x0, y0),
                    new PointF(x1, y1),
                    new PointF(x1, y0),
                    new PointF(x0, y1)
                };
            }
            else
            {
                points = new[]
                {
                    new PointF(x0, y0),
                    new PointF(x1, y1),
                    new PointF(x0, y1),
                    new PointF(x1, y0)
                };
            }

            return points;
        }
    }
}
