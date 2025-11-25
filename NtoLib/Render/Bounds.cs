using System.Drawing;

namespace NtoLib.Render
{
	public struct Bounds
	{
		/// <summary>Координата X пивота</summary>
		public float X;
		/// <summary>Координата Y пивота</summary>
		public float Y;

		/// <summary>Ширина</summary>
		public float Width;
		/// <summary>Высота</summary>
		public float Height;

		/// <summary>Пивот - относительное положение "центра масс" объекта. 
		/// X и Y задаются в форме от 0 до 1 и означают положение "центра масс"
		/// относительно ширины и высоты
		/// </summary>
		public PointF Pivot;



		/// <summary>Координата X левого края</summary>
		public float Left => X - Width * Pivot.X;
		/// <summary>Координата X пправого края</summary>
		public float Right => X + Width * (1 - Pivot.X);
		/// <summary>Координата X середины</summary>
		public float CenterX => X - Width * Pivot.X + Width / 2f;

		/// <summary>Координата Y верхей границы</summary>
		public float Top => Y - Height * Pivot.Y;
		/// <summary>Координата Y нижней границы</summary>
		public float Bottom => Y + Height * (1 - Pivot.Y);
		/// <summary>Координата Y середины</summary>
		public float CenterY => Y - Height * Pivot.Y + Height / 2f;

		/// <summary>Левый верхний угол</summary>
		public PointF LeftTop => new PointF(Left, Top);
		/// <summary>Левый нижний угол</summary>
		public PointF LeftBottom => new PointF(Left, Bottom);
		/// <summary>Правый верхний угол</summary>
		public PointF RightTop => new PointF(Right, Top);
		/// <summary>Правый нижний угол</summary>
		public PointF RightBottom => new PointF(Right, Bottom);

		/// <summary>Середина левой границы</summary>
		public PointF LeftCenter => new PointF(Left, CenterY);
		/// <summary>Середина верхней границы</summary>
		public PointF CenterTop => new PointF(CenterX, Top);
		/// <summary>Середина правой границы</summary>
		public PointF RightCenter => new PointF(Right, CenterY);
		/// <summary>Середина нижней границы</summary>
		public PointF CenterBottom => new PointF(CenterX, Bottom);

		/// <summary>Середина</summary>
		public PointF Center => new PointF(CenterX, CenterY);



		/// <summary>
		/// Создаёт экземпляр Bounds на основе RectangleF
		/// </summary>
		public static Bounds FromRectangle(RectangleF rectangle, PointF pivot)
		{
			Bounds bounds = new Bounds();
			bounds.Pivot = pivot;

			bounds.X = rectangle.X + rectangle.Width * pivot.X;
			bounds.Y = rectangle.Y + rectangle.Height * pivot.Y;

			bounds.Width = rectangle.Width;
			bounds.Height = rectangle.Height;

			return bounds;
		}



		/// <summary>
		/// Изменяет ширину и высоту, умножая их на коэффициент,
		/// и возвращает получившиеся границы
		/// </summary>
		public Bounds Resize(float sizeFactor)
		{
			Bounds result = this;
			result.Width *= sizeFactor;
			result.Height *= sizeFactor;
			return result;
		}

		/// <summary>
		/// Изменяет ширину и высоту, умножая их на соответствующие
		/// относительные значения, и возвращает получившиеся границы
		/// </summary>
		public Bounds Resize(float widthFactor, float heightFactor)
		{
			Bounds result = this;
			result.Width *= widthFactor;
			result.Height *= heightFactor;
			return result;
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


		/// <summary>
		/// Возвращает RectangleF на основе данного экземпляра Bounds
		/// </summary>
		/// <returns></returns>
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
