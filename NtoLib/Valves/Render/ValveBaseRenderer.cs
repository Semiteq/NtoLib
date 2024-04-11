using NtoLib.Render;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NtoLib.Valves.Render
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
        public abstract void Draw(Graphics graphics, PaintData data);



        /// <summary>
        /// Отрисовывает рамку ошибки
        /// </summary>
        protected void DrawErrorRectangle(Graphics graphics, Bounds graphicsBound)
        {
            PointF[] errorPoints = graphicsBound.GetPoints(- ErrorLineWidth / 2f);
            using(Pen errorPen = new Pen(RenderParams.ColorError, ErrorLineWidth))
                graphics.DrawClosedCurve(errorPen, errorPoints, 0, FillMode.Alternate);
        }

        /// <summary>
        /// Возвращает границы, в которых должен быть отрисован клапан/шибер
        /// </summary>
        protected Bounds GetValveBounds(PaintData data)
        {
            Bounds bounds = data.Bounds;
            float offset = 2f * (ErrorLineWidth + ErrorOffset);
            bounds.Width -= offset;
            bounds.Height -= offset;
            return bounds;
        }

        ///// <summary>
        ///// Возвращает точки для отрисовки по ним рамки ошибки
        ///// </summary>
        //private Bounds GetErrorBounds(Bounds valveBounds)
        //{
        //    Bounds errorBounds = valveBounds;
        //    float offset = 2f * (ErrorLineWidth + ErrorOffset);
        //    errorBounds.Width += offset;
        //    errorBounds.Height += offset;
        //    return errorBounds;
        //}

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
