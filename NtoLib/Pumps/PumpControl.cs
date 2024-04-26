using FB.VisualFB;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NtoLib.Pumps
{
    [ComVisible(true)]
    [Guid("664141D1-44B3-43D7-9897-3A10C936315A")]
    [DisplayName("Насос")]
    public partial class PumpControl : VisualControlBase
    {
        private Orientation _orientation;
        [DisplayName("Ориентация")]
        public Orientation Orientation
        {
            get
            {
                return _orientation;
            }
            set
            {
                if(_orientation != value)
                    (Width, Height) = (Height, Width);

                _orientation = value;
            }
        }



        public PumpControl()
        {
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();

            InitializeComponent();
        }
    }
}
