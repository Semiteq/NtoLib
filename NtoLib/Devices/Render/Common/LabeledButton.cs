using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using NtoLib.Devices.Helpers;

namespace NtoLib.Devices.Render.Common;

public partial class LabeledButton : Button
{
	private Color _enabledBackColor;
	private Color _enabledForeColor;

	public LabeledButton()
	{
		InitializeComponent();

		MouseDown += OnPress;
		MouseClick += OnRelease;
		Leave += OnLeave;

		EnabledChanged += HandleEnabledChanged;
	}

	public SymbolType SymbolOnButton { get; set; }

	public bool IsButtonPressed { get; private set; }

	protected override void OnPaint(PaintEventArgs e)
	{
		Text = string.Empty;

		base.OnPaint(e);

		var boundsRect = Bounds;
		boundsRect.X = 0;
		boundsRect.Y = 0;

		var bounds = Common.Bounds.FromRectangle(boundsRect, new PointF(0.5f, 0.5f));
		var size = Math.Min(bounds.Width, bounds.Height);
		bounds.X -= 0.5f;
		bounds.Y -= 0.5f;
		bounds.Width = (size - 4) * 0.75f;
		bounds.Height = (size - 4) * 0.75f;

		if (IsButtonPressed)
		{
			bounds.X += 1;
			bounds.Y += 1;
		}

		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		switch (SymbolOnButton)
		{
			case SymbolType.On:
			{
				bounds.Width = bounds.Height / 7f;
				using (var brush = new SolidBrush(ForeColor))
				{
					e.Graphics.FillRectangle(brush, bounds.ToRectangleF());
				}

				break;
			}
			case SymbolType.Off:
			{
				using (var brush = new SolidBrush(ForeColor))
				{
					e.Graphics.FillEllipse(brush, bounds.ToRectangleF());
				}

				var resizeFactor = 5f / 7f;
				bounds = bounds.Resize(resizeFactor);
				using (var brush = new SolidBrush(BackColor))
				{
					e.Graphics.FillEllipse(brush, bounds.ToRectangleF());
				}

				break;
			}
			case SymbolType.SmoothOpen:
			{
				bounds = bounds.Resize(0.75f);
				using (var brush = new SolidBrush(ForeColor))
				{
					e.Graphics.FillRectangle(brush, bounds.ToRectangleF());
				}

				break;
			}
		}
	}

	private void OnPress(object sender, MouseEventArgs e)
	{
		if (e.Button != MouseButtons.Left)
		{
			return;
		}

		IsButtonPressed = true;
	}

	private void OnRelease(object sender, MouseEventArgs e)
	{
		if (e.Button != MouseButtons.Left)
		{
			return;
		}

		IsButtonPressed = false;
		Invalidate();
	}

	private void OnLeave(object senser, EventArgs e)
	{
		IsButtonPressed = false;
	}

	private void HandleEnabledChanged(object sender, EventArgs e)
	{
		if (Enabled)
		{
			FlatStyle = FlatStyle.Standard;

			ForeColor = _enabledForeColor;
			BackColor = _enabledBackColor;
		}
		else
		{
			FlatStyle = FlatStyle.Flat;

			_enabledForeColor = ForeColor;
			_enabledBackColor = BackColor;

			ForeColor = Color.DimGray;
			BackColor = Color.Gainsboro;
		}
	}
}
