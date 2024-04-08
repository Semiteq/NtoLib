using System.Drawing;

namespace NtoLib.Valves.Render
{
    internal abstract class BaseRenderer
    {
        protected ValveControl ValveControl { get; private set; }



        public BaseRenderer(ValveControl valveControl)
        {
            ValveControl = valveControl;
        }



        public abstract void Draw(Graphics graphics, PaintData data);



        /// <summary>
        /// Возвращает границы, в которых должен быть отрисован клапан/шибер
        /// </summary>
        protected RectangleF GetElementRect(PaintData data)
        {
            RectangleF clampedBounds = data.Bounds;
            clampedBounds.Width -= 2f * (data.ErrorLineWidth + data.ErrorOffset);
            clampedBounds.Height -= 2f * (data.ErrorLineWidth + data.ErrorOffset);
            clampedBounds.X = (data.Bounds.Width - clampedBounds.Width) / 2f;
            clampedBounds.Y = (data.Bounds.Height - clampedBounds.Height) / 2f;
            return clampedBounds;
        }

        /// <summary>
        /// Возвращает точки для отрисовки по ним рамки ошибки
        /// </summary>
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

        /// <summary>
        /// Возвращает цвета первого и второго треугольника в зависимости от статуса
        /// </summary>
        protected Color[] GetValveColors(Status status, bool isLight)
        {
            Color[] colors = new Color[2];

            if(status.State == State.NoData)
            {
                colors[0] = RenderParams.ColorNoData;
                colors[1] = RenderParams.ColorNoData;
            }
            else if(status.State == State.Opened)
            {
                colors[0] = RenderParams.ColorOpened;
                colors[1] = RenderParams.ColorOpened;
            }
            else if(status.State == State.Closed || status.State == State.SmothlyOpened)
            {
                colors[0] = RenderParams.ColorClosed;
                colors[1] = RenderParams.ColorClosed;
            }
            else
            {
                if(isLight)
                {
                    colors[0] = RenderParams.ColorOpened;
                    colors[1] = RenderParams.ColorClosed;
                }
                else
                {
                    colors[0] = RenderParams.ColorClosed;
                    colors[1] = RenderParams.ColorOpened;
                }
            }

            return colors;
        }
    }
}
