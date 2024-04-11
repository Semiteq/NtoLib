using NtoLib.Valves;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NtoLib.Render.Valves
{
    internal abstract class ValveBaseRenderer
    {
        /// <summary>Толщина всех линий, кроме линии ошибки</summary>
        public float LineWidth { get; protected set; }

        /// <summary>Толщина линии ошибки</summary>
        public float ErrorLineWidth { get; protected set; }
        /// <summary>Отступ от границ клапана до рамки ошибки</summary>
        public float ErrorOffset { get; protected set; }


        /// <summary>Экземпляр ValveControl, к которому привязан данный Renderer</summary>
        protected ValveControl Control { get; private set; }



        public ValveBaseRenderer(ValveControl valveControl)
        {
            Control = valveControl;
        }



        /// <summary>
        /// Метод для отрисовки объекта Renderer'ом
        /// </summary>
        public abstract void Draw(Graphics graphics, RectangleF boundsRect, Orientation orientation, bool isLight);



        /// <summary>
        /// Возвращает границы рисования преобразованные из RectangleF в Bounds
        /// </summary>
        /// <param name="boundsRect"></param>
        /// <returns></returns>
        protected Bounds BoundsFromRect(RectangleF boundsRect)
        {
            boundsRect.X = -0.5f;
            boundsRect.Y = -0.5f;

            PointF pivot = new PointF(0.5f, 0.5f);

            return Bounds.FromRectangle(boundsRect, pivot);
        }

        /// <summary>
        /// Отрисовывает рамку ошибки
        /// </summary>
        protected void DrawErrorRectangle(Graphics graphics, Bounds graphicsBound)
        {
            PointF[] errorPoints = graphicsBound.GetPoints(-ErrorLineWidth / 2f);
            using(Pen errorPen = new Pen(RenderParams.ColorError, ErrorLineWidth))
                graphics.DrawClosedCurve(errorPen, errorPoints, 0, FillMode.Alternate);
        }

        /// <summary>
        /// Возвращает границы, в которых должен быть отрисован клапан/шибер
        /// </summary>
        protected Bounds GetValveBounds(Bounds graphicsBounds)
        {
            Bounds valveBounds = graphicsBounds;

            float offset = 2f * (ErrorLineWidth + ErrorOffset);
            valveBounds.Width -= offset;
            valveBounds.Height -= offset;

            return valveBounds;
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
    }
}
