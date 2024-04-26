using System.Windows.Forms;

namespace NtoLib.Pumps.Settings
{
    public partial class PumpSettingForm : Form
    {
        private PumpControl _pumpControl;



        public PumpSettingForm(PumpControl pumpControl)
        {
            _pumpControl = pumpControl;

            InitializeComponent();
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            Status status = _pumpControl.Status;

            lampAuto.Active = status.UsedByAutoMode;
            lampManual.Active = !status.UsedByAutoMode && status.ConnectionOk;

            useLamp.Active = status.Use;

            blockStartLamp.Active = status.BlockStart;
            lampBlockStop.Active = status.BlockStop;
            lampConnectionNotOk.Active = status.Use && !status.ConnectionOk;
            lampAnyError.Active = status.Use && (status.MainError || status.Error1 || status.Error2 || status.Error3 || status.Error4);

            base.OnPaint(e);
        }
    }
}
