using System.Windows.Forms;

namespace NtoLib.Valves.Settings
{
    public partial class SettingsForm : Form
    {
        private ValveControl _valveControl;



        public SettingsForm(ValveControl valveCotrol)
        {
            _valveControl = valveCotrol;

            InitializeComponent();

            string[] splittedString = valveCotrol.FBConnector.FBName.Split('.');
            string name = splittedString[splittedString.Length - 1];
            Text = name;
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            Status status = _valveControl.Status;

            lampOpened.Active = status.State == State.Opened;
            lampClosed.Active = status.State == State.Closed;

            lampBlockOpening.Active = status.BlockOpening;
            lampBlockClosing.Active = status.BlockClosing;


            lampAuto.Active = status.AutoMode;
            lampManual.Active = !status.AutoMode && !status.NoConnection;
            lampNotOpened.Active = status.NotOpened;
            lampNotClosed.Active = status.NotClosed;
            lampCollision.Active = status.Collision;

            base.OnPaint(e);
        }
    }
}
