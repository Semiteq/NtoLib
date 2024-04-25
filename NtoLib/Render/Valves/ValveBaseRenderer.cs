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
        protected void DrawErrorRectangle(Graphics graphics, Bounds errorBounds)
        {
            PointF[] errorPoints = errorBounds.GetPoints(-ErrorLineWidth / 2f);
            using(Pen errorPen = new Pen(Colors.Error, ErrorLineWidth))
                graphics.DrawClosedCurve(errorPen, errorPoints, 0, FillMode.Alternate);
        }

        /// <summary>
        /// Отрисовывает прямоугольник блокировки
        /// </summary>
        protected void DrawBlockRectangle(Graphics graphics, Bounds errorBoudns)
        {
            RectangleF blockRect = errorBoudns.ToRectangleF();
            using(SolidBrush brush = new SolidBrush(Colors.Blocked))
                graphics.FillRectangle(brush, blockRect);
        }

        /// <summary>
        /// Возвращает цвета первого и второго треугольника в зависимости от статуса
        /// </summary>
        protected Color[] GetValveColors(Status status, bool isLight)
        {
            Color[] colors = new Color[2];

            if(status.State == State.NoData)
            {
                colors[0] = Colors.NoData;
                colors[1] = Colors.NoData;
            }
            else if(status.State == State.Opened)
            {
                colors[0] = Colors.Opened;
                colors[1] = Colors.Opened;
            }
            else if(status.State == State.Closed || status.State == State.SmothlyOpened)
            {
                colors[0] = Colors.Closed;
                colors[1] = Colors.Closed;
            }
            else
            {
                if(isLight)
                {
                    colors[0] = Colors.Opened;
                    colors[1] = Colors.Closed;
                }
                else
                {
                    colors[0] = Colors.Closed;
                    colors[1] = Colors.Opened;
                }
            }

            return colors;
        }

        /// <summary>
        /// Возвращет истину, если должна быть показана блокировка
        /// </summary>
        protected bool IsBlocked(Status status)
        {
            if(status.UsedByAutoMode)
                return true;
            if(status.State == State.Opened && status.BlockClosing)
                return true;
            if(status.State == State.Closed && status.BlockOpening)
                return true;
            if(status.State == State.SmothlyOpened && (status.BlockClosing || status.BlockOpening))
                return true;

            return false;
        }
    }
}
