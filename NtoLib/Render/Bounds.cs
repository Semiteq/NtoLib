using System.Drawing;

namespace NtoLib.Render
{
    public struct Bounds
    {
        public float X;
        public float Y;

        public float Width;
        public float Height;

        public PointF Pivot;



        public float Left => X - Width * Pivot.X;
        public float Right => X + Width * (1 - Pivot.X);
        public float CenterX => X - Width * Pivot.X + Width / 2f;

        public float Top => Y - Height * Pivot.Y;
        public float Bottom => Y + Height * (1 - Pivot.Y);
        public float CenterY => Y - Height * Pivot.Y + Height / 2f;

        public PointF LeftTop => new PointF(Left, Top);
        public PointF LeftBottom => new PointF(Left, Bottom);
        public PointF RightTop => new PointF(Right, Top);
        public PointF RightBottom => new PointF(Right, Bottom);

        public PointF LeftCenter => new PointF(Left, CenterY);
        public PointF CenterTop => new PointF(CenterX, Top);
        public PointF RightCenter => new PointF(Right, CenterY);
        public PointF CenterBottom => new PointF(CenterX, Bottom);

        public PointF Center => new PointF(CenterX, CenterY);



        public static Bounds FromRectangle(RectangleF rectangle, PointF pivot)
        {
            Bounds bounds = new Bounds();
            bounds.Pivot = pivot;

            bounds.X =  rectangle.X + rectangle.Width * pivot.X;
            bounds.Y =  rectangle.Y + rectangle.Height * pivot.Y;

            bounds.Width = rectangle.Width;
            bounds.Height = rectangle.Height;

            return bounds;
        }



        /// <summary>
        /// Возвращает массив точек, представляющих собой 4 угла данной области
        /// </summary>
        public PointF[] GetPoints()
        {
            return new PointF[] {
                LeftTop,
                LeftBottom,
                RightBottom,
                RightTop
            };
        }

        /// <summary>
        /// Возвращает массив точек, представляющих собой 4 угла данной области 
        /// с заданным отступом по направлению от центра
        /// </summary>
        /// <param name="offset">
        /// Отступ. Положительный - точки будут дальше от центра, чем реальные углы.
        /// Отрицательный - точки будут ближе к центру, чем реальные углы
        /// </param>
        public PointF[] GetPoints(float offset)
        {
            return new PointF[] {
                new PointF(Left - offset, Top - offset),
                new PointF(Left - offset, Bottom + offset),
                new PointF(Right + offset, Bottom + offset),
                new PointF(Right + offset, Top - offset)
            };
        }


        public RectangleF ToRectangleF()
        {
            RectangleF rectangle = new RectangleF();
            rectangle.X = Left;
            rectangle.Y = Top;
            rectangle.Width = Width;
            rectangle.Height = Height;
            return rectangle;
        }
    }
}
