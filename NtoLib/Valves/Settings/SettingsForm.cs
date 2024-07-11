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
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            Status status = _valveControl.Status;

            openedLamp.Active = status.Opened;
            closedLamp.Active = status.Closed;

            blockOpeningLamp.Active = status.BlockOpening;
            blockClosingLamp.Active = status.BlockClosing;
            forceCloseLamp.Active = status.ForceClose;

            noConnectionLamp.Visible = !status.ConnectionOk;
            notOpenedLamp.Visible = status.NotOpened;
            notClosedLamp.Visible = status.NotClosed;
            unknownStateLamp.Visible = status.UnknownState;
            collisionLamp.Visible = status.Collision;

            base.OnPaint(e);
        }
    }
}
