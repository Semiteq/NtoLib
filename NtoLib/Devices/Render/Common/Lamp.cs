using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using NtoLib.Devices.Helpers;

namespace NtoLib.Devices.Render.Common;

public partial class Lamp : UserControl
{
	public Shape Shape { get; set; }

	private Color _activeColor;

	public Color ActiveColor
	{
		get => _activeColor;
		set
		{
			_activeColor = value;

			if (_active)
				Invalidate();
		}
	}

	public string TextOnLamp { get; set; } = string.Empty;

	private bool _active;

	public bool Active
	{
		get => _active;
		set
		{
			if (_active != value)
			{
				_active = value;
				Invalidate();
			}
		}
	}


	public Lamp()
	{
		SetStyle(ControlStyles.UserPaint, true);
		SetStyle(ControlStyles.AllPaintingInWmPaint, true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		UpdateStyles();

		Margin = new Padding(5);

		InitializeComponent();
	}


	protected override void OnPaint(PaintEventArgs e)
	{
		var localBounds = new Rectangle(0, 0, Bounds.Width - 1, Bounds.Height - 1);
		var pen = new Pen(Color.Black, 1f);
		Brush brush;

		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

		if (Active)
		{
			brush = new SolidBrush(ActiveColor);
		}
		else
		{
			brush = new SolidBrush(Color.Transparent);
		}

		if (Shape == Shape.Circle)
		{
			e.Graphics.FillEllipse(brush, localBounds);
			e.Graphics.DrawEllipse(pen, localBounds);
		}
		else if (Shape == Shape.Square)
		{
			e.Graphics.FillRectangle(brush, localBounds);
			e.Graphics.DrawRectangle(pen, localBounds);
		}


		if (TextOnLamp != string.Empty)
		{
			var format = StringFormat.GenericDefault;
			format.Alignment = StringAlignment.Center;
			format.LineAlignment = StringAlignment.Center;

			e.Graphics.DrawString(TextOnLamp, Font, Brushes.Black, localBounds, format);
		}

		base.OnPaint(e);
	}
}
