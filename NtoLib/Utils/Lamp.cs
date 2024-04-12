using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NtoLib.Utils
{
    public partial class Lamp : UserControl
    {
        public Color ActiveColor { get; set; }

        private bool _active;
        public bool Active
        {
            get
            {
                return _active;
            }
            set
            {
                if(_active != value)
                {
                    _active = value;
                    this.Invalidate();
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
            RectangleF localBounds = new RectangleF(0, 0, Bounds.Width - 1, Bounds.Height - 1);
            Pen pen = new Pen(Color.Black, 1f);
            Brush brush;


            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if(Active)
                brush = new SolidBrush(ActiveColor);
            else
                brush = new SolidBrush(Color.Transparent);
            e.Graphics.FillEllipse(brush, localBounds);
            e.Graphics.DrawEllipse(pen, localBounds);

            base.OnPaint(e);
        }
    }
}
