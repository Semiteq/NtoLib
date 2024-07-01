using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NtoLib.Utils
{
    public partial class LabledButton : Button
    {
        public SymbolType SymbolOnButton { get; set; }

        public bool IsButtonPressed { get; private set; }



        public LabledButton()
        {
            InitializeComponent();

            MouseDown += OnPress;
            MouseClick += OnRelease;
            Leave += OnLeave;
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            Text = string.Empty;

            base.OnPaint(e);

            Rectangle boundsRect = Bounds;
            boundsRect.X = 0;
            boundsRect.Y = 0;

            Render.Bounds bounds = Render.Bounds.FromRectangle(boundsRect, new PointF(0.5f, 0.5f));
            float size = Math.Min(bounds.Width, bounds.Height);
            bounds.Width = size * 0.6f;
            bounds.Height = size * 0.6f;

            if(IsButtonPressed)
            {
                bounds.X += 1;
                bounds.Y += 1;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            switch(SymbolOnButton)
            {
                case SymbolType.On:
                {
                    bounds.Width = bounds.Height / 7f;
                    using(SolidBrush brush = new SolidBrush(ForeColor))
                        e.Graphics.FillRectangle(brush, bounds.ToRectangleF());

                    break;
                }
                case SymbolType.Off:
                {
                    using(SolidBrush brush = new SolidBrush(ForeColor))
                        e.Graphics.FillEllipse(brush, bounds.ToRectangleF());

                    float resizeFactor = 5f / 7f;
                    bounds.Width = bounds.Width * resizeFactor;
                    bounds.Height = bounds.Height * resizeFactor;
                    using(SolidBrush brush = new SolidBrush(BackColor))
                        e.Graphics.FillEllipse(brush, bounds.ToRectangleF());

                    break;
                }
                case SymbolType.SmoothOpen:
                {
                    //Тут квадратик поменьше сделать

                    using(SolidBrush brush = new SolidBrush(ForeColor))
                        e.Graphics.FillRectangle(brush, bounds.ToRectangleF());

                    break;
                }
            }
        }
        
        private void OnPress(object sender, MouseEventArgs e)
        {
            if(e.Button != MouseButtons.Left)
                return;

            IsButtonPressed = true;
        }

        private void OnRelease(object sender, MouseEventArgs e)
        {
            if(e.Button != MouseButtons.Left)
                return;

            IsButtonPressed = false;
            Invalidate();
        }

        private void OnLeave(object senser,  EventArgs e)
        {
            IsButtonPressed = false;
        }
    }
}
