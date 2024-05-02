using System.Windows.Forms;

namespace NtoLib.Pumps.Settings
{
    public partial class PumpSettingForm : Form
    {
        private PumpControl _pumpControl;
        private PumpType _pumpType;



        public PumpSettingForm(PumpControl pumpControl)
        {
            _pumpControl = pumpControl;

            InitializeComponent();

            PumpFB pumpFB = _pumpControl.FBConnector.Fb as PumpFB;
            if(!pumpFB.UseNoConnectionLamp)
                flowLayoutPanel1.Controls.Remove(noConnectionLamp);

            if(!pumpFB.UseTemperatureLabel)
                flowLayoutPanel1.Controls.Remove(temperatureLabel);

            _pumpType = pumpFB.PumpType;
            if(_pumpType != PumpType.Turbine)
            {
                flowLayoutPanel1.Controls.Remove(speedLabel);
            }
            if(_pumpType != PumpType.Ion)
            {
                flowLayoutPanel1.Controls.Remove(safeModeLamp);

                flowLayoutPanel1.Controls.Remove(voltageLabel);
                flowLayoutPanel1.Controls.Remove(currentLabel);
                flowLayoutPanel1.Controls.Remove(powerLabel);
            }
            if(_pumpType != PumpType.Cryogen)
            {
                flowLayoutPanel1.Controls.Remove(temperatureInLabel);
                flowLayoutPanel1.Controls.Remove(temperatureOutLabel);
            }
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

            temperatureLabel.Text =     $"Температура: {status.Temperature} K";
                                                       
            switch(_pumpType)                          
            {                                          
                case PumpType.Forvacuum :              
                {                                      
                                                       
                    break;                             
                }                                      
                case PumpType.Turbine:
                {
                    string units = status.Units ? "об/мин" : "%";
                    speedLabel.Text =   $"Скорость: {status.Speed} {units}";

                    break;                             
                }                                      
                case PumpType.Ion:                     
                {
                    safeModeLamp.Active = status.SafeMode;

                    voltageLabel.Text = $"Напряжение: {status.Voltage} В";
                    currentLabel.Text = $"Ток: {status.Current} А";
                    powerLabel.Text =   $"Мощность: {status.Power} Вт";
                    break;                             
                }                                      
                case PumpType.Cryogen:                 
                {                                      
                    powerLabel.Text =   $"Твх: {status.Power} К";
                    powerLabel.Text =   $"Твых: {status.Power} К";
                    break;
                }
            }


            forceStopLamp.Active = status.ForceStop;
            blockStartLamp.Active = status.BlockStart;
            blockStopLamp.Active = status.BlockStop;
            noConnectionLamp.Active = status.Use && !status.ConnectionOk;
            errorLamp.Active = status.Use && status.MainError;

            base.OnPaint(e);
        }
    }
}
