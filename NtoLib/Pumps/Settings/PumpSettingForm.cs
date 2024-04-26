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

            string state = string.Empty;
            if(!status.Use)
                state = "не используется";
            else if(!status.ConnectionOk)
                state = "нет связи";
            else if(status.Accelerating)
                state = "разгоняется";
            else if(status.Decelerating)
                state = "тормозит";
            else if(status.WorkOnNominalSpeed)
                state = "разогнан";
            else if(status.Stopped)
                state = "остановлен";
            stateLabel.Text = $"Состояние: {state}";

            forceStopLamp.Active = status.ForceStop;
            blockStartLamp.Active = status.BlockStart;
            lampBlockStop.Active = status.BlockStop;
            lampConnectionNotOk.Active = status.Use && !status.ConnectionOk;
            lampAnyError.Active = status.Use && status.MainError;

            base.OnPaint(e);
        }
    }
}
