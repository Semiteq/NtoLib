using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NtoLib.Render
{
    public abstract class BaseRenderer
    {
        /// <summary>Толщина всех линий, кроме линии ошибки</summary>
        public float LineWidth { get; protected set; }

        /// <summary>Толщина линии ошибки</summary>
        public float ErrorLineWidth { get; protected set; }
        /// <summary>Отступ от границ клапана до рамки ошибки</summary>
        public float ErrorOffset { get; protected set; }



        /// <summary>
        /// Метод для отрисовки объекта Renderer'ом, возвращает границы отрисовки
        /// </summary>
        public abstract Bounds Draw(Graphics graphics, RectangleF boundsRect, Orientation orientation, bool isLight);



        /// <summary>
        /// Применяет к объекту Graphics нужный поворот и возвращет границы отрисовки, развёрнутые нужным образом
        /// </summary>
        protected Bounds ConfigurateGraphics(Graphics graphics, RectangleF boundsRect, Orientation orientation)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Bounds graphicsBounds = BoundsFromRect(boundsRect);
            if(orientation != Orientation.Right)
            {
                Matrix transform = graphics.Transform;
                transform.RotateAt((int)orientation, graphicsBounds.Center);
                graphics.Transform = transform;
                transform.Dispose();

                if(orientation == Orientation.Bottom || orientation == Orientation.Top)
                    (graphicsBounds.Width, graphicsBounds.Height) = (graphicsBounds.Height, graphicsBounds.Width);
            }

            return graphicsBounds;
        }

        /// <summary>
        /// Возвращает границы рисования преобразованные из RectangleF в Bounds
        /// </summary>
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
    }
}
