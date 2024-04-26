using System.Windows.Forms;

namespace NtoLib.Pumps.Settings
{
    public partial class PumpSettingForm : Form
    {
        private PumpControl _pumpControl;



        public PumpSettingForm(PumpControl pumpControl, bool useNoConnectionLamp)
        {
            _pumpControl = pumpControl;

            InitializeComponent();

            if(!useNoConnectionLamp)
                flowLayoutPanel1.Controls.Remove(noConnectionLamp);
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            Status status = _pumpControl.Status;

            lampAuto.Active = status.UsedByAutoMode;
            lampManual.Active = !status.UsedByAutoMode && status.ConnectionOk;

            string state = string.Empty;
            if(!status.Use || !status.ConnectionOk)
                state = "нет данных";
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
            blockStopLamp.Active = status.BlockStop;
            noConnectionLamp.Active = status.Use && !status.ConnectionOk;
            errorLamp.Active = status.Use && status.MainError;

            base.OnPaint(e);
        }
    }
}
