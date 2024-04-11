using System.Windows.Forms;

namespace NtoLib.Valves
{
    public partial class SettingsForm : Form
    {
        private ValveControl _valveControl;



        public SettingsForm(ValveControl valveCotrol)
        {
            _valveControl = valveCotrol;

            InitializeComponent();
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            Status status = _valveControl.Status;

            lampOpened.Active = status.State == State.Opened;
            lampClosed.Active = status.State == State.Closed;
            lampBlockOpening.Active = status.BlockOpening;
            lampBlockClosing.Active = status.BlockClosing;

            base.OnPaint(e);
        }
    }
}
