using System;
using System.Windows.Forms;

namespace NtoLib.Devices.Pumps.Settings
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

            string state = GetStateString(_pumpType, status);
            stateLabel.Text = $"Состояние: {state}";

            temperatureLabel.ValueText = $"{status.Temperature.ToString("F0")} C°";

            switch(_pumpType)
            {
                case PumpType.Forvacuum:
                {

                    break;
                }
                case PumpType.Turbine:
                {
                    string units = status.Units ? "%" : "об/мин";
                    speedLabel.ValueText = $"{status.Speed.ToString("F1")} {units}";

                    break;
                }
                case PumpType.Ion:
                {
                    safeModeLamp.Active = status.SafeMode;

                    voltageLabel.ValueText = $"{status.Voltage.ToString("F2")} В";
                    currentLabel.ValueText = $"{status.Current.ToString("F2")} А";
                    powerLabel.ValueText = $"{status.Power.ToString("F2")} Вт";
                    break;
                }
                case PumpType.Cryogen:
                {
                    temperatureInLabel.ValueText = $"{status.TemperatureIn.ToString("F2")} К";
                    temperatureOutLabel.ValueText = $"{status.TemperatureOut.ToString("F2")} К";
                    break;
                }
            }


            forceStopLamp.Active = status.ForceStop;
            blockStartLamp.Active = status.BlockStart;
            blockStopLamp.Active = status.BlockStop;
            noConnectionLamp.Active = status.Use && !status.ConnectionOk;
            errorLamp.Active = status.Use && status.MainError;
            warningLamp.Active = status.Warning;

            base.OnPaint(e);
        }

        private string GetStateString(PumpType pumpType, Status status)
        {
            if(!status.Use || !status.ConnectionOk)
                return "нет данных";

            switch(pumpType)
            {
                case PumpType.Forvacuum:
                {
                    if(status.Accelerating)
                        return "разгон";
                    else if(status.Decelerating)
                        return "замедление";
                    else if(status.WorkOnNominalSpeed)
                        return "рабочий режим";
                    else if(status.Stopped)
                        return "остановлен";

                    throw new NotImplementedException();
                }
                case PumpType.Turbine:
                {
                    if(status.Accelerating)
                        return "разгон";
                    else if(status.Decelerating)
                        return "замедление";
                    else if(status.WorkOnNominalSpeed)
                        return "рабочий режим";
                    else if(status.Stopped)
                        return "остановлена";

                    throw new NotImplementedException();
                }
                case PumpType.Ion:
                {
                    if(status.Accelerating)
                        return "охлаждение";
                    else if(status.Decelerating)
                        return "нагрев";
                    else if(status.WorkOnNominalSpeed)
                        return "рабочий режим";
                    else if(status.Stopped)
                        return "остановлен";

                    throw new NotImplementedException();
                }
                case PumpType.Cryogen:
                {
                    if(status.Accelerating)
                        return "повышение напряжения";
                    else if(status.Decelerating)
                        return "выключение";
                    else if(status.WorkOnNominalSpeed)
                        return "рабочий режим";
                    else if(status.Stopped)
                        return "остановлен";

                    throw new NotImplementedException();
                }
                default:
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
