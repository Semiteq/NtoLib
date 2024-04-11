using System.Drawing;

namespace NtoLib.Valves.Render
{
    internal abstract class BaseRenderer
    {
        /// <summary>Толщина всех линий, кроме линии ошибки</summary>
        public float LineWidth { get; protected set; }
        /// <summary>Толщина линии ошибки</summary>
        public float ErrorLineWidth { get; protected set; }

        /// <summary>Отступ от границ клапана до рамки ошибки</summary>
        public float ErrorOffset { get; protected set; }



        /// <summary>Экземпляр ValveControl, к которому привязан данный Renderer</summary>
        protected ValveControl Control { get; private set; }



        public BaseRenderer(ValveControl valveControl)
        {
            Control = valveControl;
        }



        /// <summary>
        /// Метод для отрисовки объекта Renderer'ом
        /// </summary>
        public abstract void Draw(Graphics graphics, PaintData data);



        /// <summary>
        /// Отрисовывает рамку ошибки
        /// </summary>
        protected void DrawErrorRectangle(Graphics graphics, RectangleF valveRect, PaintData paintData)
        {
            PointF[] errorPoints = GetErrorRectPoints(valveRect, ErrorLineWidth, ErrorOffset);
            using(Pen errorPen = new Pen(RenderParams.ColorError))
                graphics.DrawLines(errorPen, errorPoints);
        }

        /// <summary>
        /// Возвращает границы, в которых должен быть отрисован клапан/шибер
        /// </summary>
        protected RectangleF GetElementRect(PaintData data)
        {
            RectangleF clampedBounds = data.Bounds;
            clampedBounds.Width -= 2f * (ErrorLineWidth + ErrorOffset);
            clampedBounds.Height -= 2f * (ErrorLineWidth + ErrorOffset);
            clampedBounds.X = (data.Bounds.Width - clampedBounds.Width) / 2f;
            clampedBounds.Y = (data.Bounds.Height - clampedBounds.Height) / 2f;
            return clampedBounds;
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

        /// <summary>
        /// Возвращает цвет обводки клапана в зависимости от его статуса
        /// </summary>
        protected Color GetLineColor(Status status)
        {
            State state = status.State;
            if((state == State.Opened && status.BlockClosing) || (state == State.Closed && status.BlockOpening))
                return RenderParams.ColorBlocked;
            else
                return RenderParams.ColorLines;
        }

        /// <summary>
        /// Возвращает точки для отрисовки по ним рамки ошибки
        /// </summary>
        private PointF[] GetErrorRectPoints(RectangleF valveRect, float errorLineWidth, float errorOffset)
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
    }
}
