using System;
using System.Windows.Forms;

namespace NtoLib.Devices.Pumps.Settings;

public partial class PumpSettingForm : Form
{
	private PumpControl _pumpControl;
	private PumpType _pumpType;

	public PumpSettingForm(PumpControl pumpControl)
	{
		_pumpControl = pumpControl;

		InitializeComponent();

		var pumpFb = _pumpControl.FBConnector.Fb as PumpFB;

		if (pumpFb == null)
		{
			return;
		}

		if (!pumpFb.UseNoConnectionLamp)
		{
			flowLayoutPanel1.Controls.Remove(noConnectionLamp);
		}

		if (!pumpFb.UseTemperatureLabel)
		{
			flowLayoutPanel1.Controls.Remove(temperatureLabel);
		}

		_pumpType = pumpFb.PumpType;

		if (_pumpType != PumpType.Turbine)
		{
			flowLayoutPanel1.Controls.Remove(speedLabel);
		}

		if (_pumpType != PumpType.Ion)
		{
			flowLayoutPanel1.Controls.Remove(safeModeLamp);

			flowLayoutPanel1.Controls.Remove(voltageLabel);
			flowLayoutPanel1.Controls.Remove(currentLabel);
		}

		if (_pumpType != PumpType.Cryogen)
		{
			flowLayoutPanel1.Controls.Remove(temperatureInLabel);
			flowLayoutPanel1.Controls.Remove(temperatureOutLabel);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var status = _pumpControl.Status;

		var state = GetStateString(_pumpType, status);
		stateLabel.Text = $@"Состояние: {state}";

		temperatureLabel.ValueText = $"{status.Temperature:F0} C°";

		switch (_pumpType)
		{
			case PumpType.Forvacuum:
			{
				break;
			}
			case PumpType.Turbine:
			{
				speedLabel.ValueText = $"{status.Speed:F1} %";

				break;
			}
			case PumpType.Ion:
			{
				safeModeLamp.Active = status.SafeMode;

				voltageLabel.ValueText = $"{status.Voltage:F2} В";
				currentLabel.ValueText = $"{status.Current:F2} А";
				break;
			}
			case PumpType.Cryogen:
			{
				temperatureInLabel.ValueText = $"{status.TemperatureIn:F2} К";
				temperatureOutLabel.ValueText = $"{status.TemperatureOut:F2} К";
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

	private static string GetStateString(PumpType pumpType, Status status)
	{
		if (!status.Use || !status.ConnectionOk)
			return "нет данных";

		switch (pumpType)
		{
			case PumpType.Forvacuum:
			{
				if (status.Accelerating)
					return "разгон";

				if (status.Decelerating)
					return "замедление";

				if (status.WorkOnNominalSpeed)
					return "рабочий режим";

				if (status.Stopped)
					return "остановлен";

				if (!status.Accelerating || status.Decelerating || !status.WorkOnNominalSpeed || !status.Stopped)
					return "не определено";

				throw new NotImplementedException();
			}
			case PumpType.Turbine:
			{
				if (status.Accelerating)
					return "разгон";

				if (status.Decelerating)
					return "замедление";

				if (status.WorkOnNominalSpeed)
					return "рабочий режим";

				if (status.Stopped)
					return "остановлена";

				if (!status.Accelerating || status.Decelerating || !status.WorkOnNominalSpeed || !status.Stopped)
					return "не определено";

				throw new NotImplementedException();
			}
			case PumpType.Ion:
			{
				if (status.Accelerating)
					return "охлаждение";

				if (status.Decelerating)
					return "нагрев";

				if (status.WorkOnNominalSpeed)
					return "рабочий режим";

				if (status.Stopped)
					return "остановлен";

				if (!status.Accelerating || status.Decelerating || !status.WorkOnNominalSpeed || !status.Stopped)
					return "не определено";

				throw new NotImplementedException();
			}
			case PumpType.Cryogen:
			{
				if (status.Accelerating)
					return "повышение напряжения";

				if (status.Decelerating)
					return "выключение";

				if (status.WorkOnNominalSpeed)
					return "рабочий режим";

				if (status.Stopped)
					return "остановлен";

				if (!status.Accelerating || status.Decelerating || !status.WorkOnNominalSpeed || !status.Stopped)
					return "не определено";

				throw new NotImplementedException();
			}
			default:
			{
				throw new NotImplementedException();
			}
		}
	}
}
