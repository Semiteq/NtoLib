using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using FB;
using FB.VisualFB;

using InSAT.Library.Interop;
using InSAT.OPC;

using MasterSCADA.Hlp;

using NtoLib.Devices.Helpers;

namespace NtoLib.Devices.Pumps;

[Serializable]
[ComVisible(true)]
[Guid("0B9D612A-E69E-4817-AA0A-878186938C3F")]
[CatID(CatIDs.CATID_OTHER)]
[DisplayName("Насос")]
[VisualControls(typeof(PumpControl))]
public class PumpFB : VisualFBBaseExtended
{
	private PumpType _pumpType;

	[DisplayName("Тип насоса")]
	public PumpType PumpType
	{
		get => _pumpType;
		set
		{
			_pumpType = value;
			RecreatePinMap();
		}
	}

	[DisplayName("Показывать лампочку \"Нет соединения\"")]
	[Description("Переключает отображение лампочки \"Нет соединения\" в окне параметров насоса")]
	public bool UseNoConnectionLamp { get; set; }

	[DisplayName("Отображать температуру")]
	[Description("Переключает отображение температуры в окне параметров насоса")]
	public bool UseTemperatureLabel { get; set; }

	public const int StatusWordId = 1;
	public const int CommandWordId = 5;

	public const int ConnectionOkId = 0;
	public const int MainErrorId = 1;
	public const int UsedByAutoModeId = 2;
	public const int WorkOnNominalSpeedId = 3;
	public const int StoppedId = 4;
	public const int AcceleratingId = 5;
	public const int DeceseratingId = 6;
	public const int WarningId = 7;
	public const int Message1Id = 8;
	public const int Message2Id = 9;
	public const int Message3Id = 10;
	public const int CustomId = 11;
	public const int ForceStopId = 12;
	public const int BlockStartId = 13;
	public const int BlockStopId = 14;
	public const int UsedId = 15;

	public const int TemperatureId = 20;

	public const int DynamicGroupId = 50;

	public const int TurbineSpeedId = 60;

	public const int IonPumpVoltage = 70;
	public const int IonPumpCurrent = 75;
	public const int IonPumpPower = 80;

	public const int CryoInTemperature = 90;
	public const int CryoOutTemperature = 95;

	public const int StartCmdId = 100;
	public const int StopCmdId = 101;

	public const int ConnectionDisabledEventId = 5000;
	private EventTrigger? _connectionDisabledEvent;

	public const int AccelerationStartEventId = 5010;
	private EventTrigger? _accelerationEvent;

	public const int DecelerationStartEventId = 5011;
	private EventTrigger? _decelerationEvent;

	public const int ReachNominalSpeedEventId = 5012;
	private EventTrigger? _nominalSpeedEvent;

	public const int StopEventId = 5013;
	private EventTrigger? _stopEvent;

	public const int UserStartEventId = 5020;
	public const int UserStopEventId = 5021;

	private bool _prevStartCmd;
	private bool _prevStopCmd;

	protected override void ToRuntime()
	{
		base.ToRuntime();

		var splittedString = FullName.Split('.');
		var name = splittedString[^1];

		var initialInactivity = TimeSpan.FromSeconds(10);
		var type = new string[3];
		switch (PumpType)
		{
			case PumpType.Forvacuum:
			{
				type[0] = "Форнасосом";
				type[1] = "Форнасоса";
				type[2] = "Форнасос";
				break;
			}
			case PumpType.Turbine:
			{
				type[0] = "Турбиной";
				type[1] = "Турбины";
				type[2] = "Турбина";
				break;
			}
			case PumpType.Ion:
			{
				type[0] = "Ионным насосом";
				type[1] = "Ионного насоса";
				type[2] = "Ионный насос";
				break;
			}
			case PumpType.Cryogen:
			{
				type[0] = "Криогенным насосом";
				type[1] = "Криогенного насоса";
				type[2] = "Криогенный насос";
				break;
			}
		}

		var message = $"Нет соединения с {type[0].ToLower()} {name}";
		_connectionDisabledEvent = new EventTrigger(this, ConnectionDisabledEventId, message, initialInactivity);

		message = $"Начало разгона {type[1].ToLower()} {name}";
		_accelerationEvent = new EventTrigger(this, AccelerationStartEventId, message, initialInactivity);

		message = $"Начало торможения {type[1].ToLower()} {name}";
		_decelerationEvent = new EventTrigger(this, DecelerationStartEventId, message, initialInactivity);

		var action = PumpType == PumpType.Turbine ? "вышла" : "вышел";
		message = $"{type[2]} {name} {action} на номинальную скорость";
		_nominalSpeedEvent = new EventTrigger(this, ReachNominalSpeedEventId, message, initialInactivity);

		action = PumpType == PumpType.Turbine ? "остановилась" : "остановился";
		message = $"{type[2]} {name} {action}";
		_stopEvent = new EventTrigger(this, StopEventId, message, initialInactivity, true);
	}

	protected override void UpdateData()
	{
		base.UpdateData();

		var statusWord = 0;
		if (GetPinQuality(StatusWordId) == OpcQuality.Ok)
			statusWord = GetPinValue<int>(StatusWordId);

		var connectionOk = statusWord.GetBit(ConnectionOkId);
		SetVisualAndUiPin(ConnectionOkId, connectionOk);
		SetVisualAndUiPin(MainErrorId, statusWord.GetBit(MainErrorId));
		SetVisualAndUiPin(UsedByAutoModeId, statusWord.GetBit(UsedByAutoModeId));
		var workOnNominalSpeed = statusWord.GetBit(WorkOnNominalSpeedId);
		SetVisualAndUiPin(WorkOnNominalSpeedId, workOnNominalSpeed);
		var stopped = statusWord.GetBit(StoppedId);
		SetVisualAndUiPin(StoppedId, stopped);
		var accelerating = statusWord.GetBit(AcceleratingId);
		SetVisualAndUiPin(AcceleratingId, accelerating);
		var decelerating = statusWord.GetBit(DeceseratingId);
		SetVisualAndUiPin(DeceseratingId, decelerating);
		SetVisualAndUiPin(WarningId, statusWord.GetBit(WarningId));
		SetVisualAndUiPin(Message1Id, statusWord.GetBit(Message1Id));
		SetVisualAndUiPin(Message2Id, statusWord.GetBit(Message2Id));
		SetVisualAndUiPin(Message3Id, statusWord.GetBit(Message3Id));
		SetVisualAndUiPin(CustomId, statusWord.GetBit(CustomId));
		SetVisualAndUiPin(ForceStopId, statusWord.GetBit(ForceStopId));
		SetVisualAndUiPin(BlockStartId, statusWord.GetBit(BlockStartId));
		SetVisualAndUiPin(BlockStopId, statusWord.GetBit(BlockStopId));
		var used = statusWord.GetBit(UsedId);
		SetVisualAndUiPin(UsedId, used);

		SetVisualPin(TemperatureId, GetPinValue<float>(TemperatureId));

		switch (PumpType)
		{
			case PumpType.Forvacuum:
			{
				break;
			}
			case PumpType.Turbine:
			{
				SetVisualPin(TurbineSpeedId, GetPinValue<float>(TurbineSpeedId));
				break;
			}
			case PumpType.Ion:
			{
				SetVisualPin(IonPumpVoltage, GetPinValue<float>(IonPumpVoltage));
				SetVisualPin(IonPumpCurrent, GetPinValue<float>(IonPumpCurrent));
				SetVisualPin(IonPumpPower, GetPinValue<float>(IonPumpPower));
				break;
			}
			case PumpType.Cryogen:
			{
				SetVisualPin(CryoInTemperature, GetPinValue<float>(CryoInTemperature));
				SetVisualPin(CryoOutTemperature, GetPinValue<float>(CryoOutTemperature));
				break;
			}
		}

		var commandWord = 0;
		var startCmd = GetVisualPin<bool>(StartCmdId);
		var stopCmd = GetVisualPin<bool>(StopCmdId);
		commandWord.SetBit(0, startCmd);
		commandWord.SetBit(1, stopCmd);
		SetPinValue(CommandWordId, commandWord);

		if (startCmd && !_prevStartCmd)
		{
			FireUserStartEvent();
		}

		if (stopCmd && !_prevStopCmd)
		{
			FireUserStopEvent();
		}

		_prevStartCmd = startCmd;
		_prevStopCmd = stopCmd;

		if (used)
		{
			_connectionDisabledEvent?.Update(!connectionOk);

			_nominalSpeedEvent?.Update(workOnNominalSpeed);
			_stopEvent?.Update(stopped);
			_accelerationEvent?.Update(accelerating);
			_decelerationEvent?.Update(decelerating);
		}
	}

	protected override void CreatePinMap(bool newObject)
	{
		base.CreatePinMap(newObject);

		var group = (FBGroup)_mapIDtoItem[DynamicGroupId];
		switch (PumpType)
		{
			case PumpType.Forvacuum:
			{
				break;
			}
			case PumpType.Turbine:
			{
				group.AddPinWithID(TurbineSpeedId, "Speed", PinType.Pin, typeof(float), 0d);
				break;
			}
			case PumpType.Ion:
			{
				group.AddPinWithID(IonPumpVoltage, "Voltage", PinType.Pin, typeof(float), 0d);
				group.AddPinWithID(IonPumpCurrent, "Current", PinType.Pin, typeof(float), 0d);
				group.AddPinWithID(IonPumpPower, "Power", PinType.Pin, typeof(float), 0d);
				break;
			}
			case PumpType.Cryogen:
			{
				group.AddPinWithID(CryoInTemperature, "TemperatureIn", PinType.Pin, typeof(float), 0d);
				group.AddPinWithID(CryoOutTemperature, "TemperatureOut", PinType.Pin, typeof(float), 0d);
				break;
			}
		}

		FirePinSpaceChanged();
	}

	protected override OpcQuality GetConnectionQuality()
	{
		return GetPinQuality(StatusWordId);
	}

	public void FireUserStartEvent()
	{
		var splittedString = FullName.Split('.');
		var name = splittedString[^1];
		var message = $"Насос {name}: оператор нажал кнопку Пуск";
		FireEvent(UserStartEventId, message);
	}

	public void FireUserStopEvent()
	{
		var splittedString = FullName.Split('.');
		var name = splittedString[^1];
		var message = $"Насос {name}: оператор нажал кнопку Стоп";
		FireEvent(UserStopEventId, message);
	}
}
