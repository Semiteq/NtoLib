using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using FB;
using FB.VisualFB;

using InSAT.Library.Interop;
using InSAT.OPC;

using NtoLib.Devices.Helpers;

namespace NtoLib.Devices.Valves;

[Serializable]
[ComVisible(true)]
[Guid("0B747EAD-4E9B-47CE-99AA-12BF8F5192A4")]
[CatID(CatIDs.CATID_OTHER)]
[DisplayName("Клапан")]
[VisualControls(typeof(ValveControl))]
public class ValveFB : VisualFBBaseExtended
{
	public const int StatusWordId = 1;
	public const int CommandWordId = 5;

	public const int ConnectionOkId = 0;
	public const int NotOpenedId = 1;
	public const int NotClosedId = 2;
	public const int CollisionId = 3;
	public const int UsedByAutoModeId = 4;
	public const int OpenedId = 5;
	public const int SmoothlyOpenedId = 6;
	public const int ClosedId = 7;
	public const int OpeningClosingId = 8;
	public const int UsedId = 9;
	public const int ManualId = 10;
	public const int WithoutSensorsId = 11;

	public const int ForceCloseId = 12;
	public const int BlockClosingId = 13;
	public const int BlockOpeningId = 14;
	public const int IsSmoothValveId = 15;

	public const int OpenCmdId = 100;
	public const int CloseCmdId = 101;
	public const int OpenSmoothlyCmdId = 102;

	public const int CollistionEventId = 5000;

	public const int NotOpenedEventId = 5001;

	public const int NotClosedEventId = 5002;

	public const int OpenedEventId = 5010;

	public const int OpenedSmoothlyEventId = 5011;

	public const int ClosedEventId = 5012;

	public const int UserOpenEventId = 5020;
	public const int UserOpenSmoothlyEventId = 5021;
	public const int UserCloseEventId = 5022;
	private EventTrigger? _closedEvent;
	private EventTrigger? _collisionEvent;
	private EventTrigger? _notClosedEvent;
	private EventTrigger? _notOpenedEvent;
	private EventTrigger? _openedEvent;
	private EventTrigger? _openedSmoothlyEvent;
	private bool _prevCloseCmd;

	private bool _prevOpenCmd;
	private bool _prevOpenSmoothlyCmd;

	protected override void ToRuntime()
	{
		base.ToRuntime();

		var splittedString = FullName.Split('.');
		var name = splittedString[splittedString.Length - 1];

		var initialInactivity = TimeSpan.FromSeconds(10);

		var message = $"Коллизия концевиков у {name}";
		_collisionEvent = new EventTrigger(this, CollistionEventId, message, initialInactivity);

		message = $"{name} не открылся";
		_notOpenedEvent = new EventTrigger(this, NotOpenedEventId, message, initialInactivity);

		message = $"{name} не закрылся";
		_notClosedEvent = new EventTrigger(this, NotClosedEventId, message, initialInactivity);

		message = $"{name} открылся";
		_openedEvent = new EventTrigger(this, OpenedEventId, message, initialInactivity, true);

		message = $"{name} плавно открылся";
		_openedSmoothlyEvent = new EventTrigger(this, OpenedSmoothlyEventId, message, initialInactivity, true);

		message = $"{name} закрылся";
		_closedEvent = new EventTrigger(this, ClosedEventId, message, initialInactivity, true);
	}

	protected override void UpdateData()
	{
		base.UpdateData();

		var statusWord = 0;
		if (GetPinQuality(StatusWordId) == OpcQuality.Ok)
		{
			statusWord = GetPinValue<int>(StatusWordId);
		}

		var connectionOk = statusWord.GetBit(ConnectionOkId);
		SetVisualAndUiPin(ConnectionOkId, connectionOk);

		var notOpened = statusWord.GetBit(NotOpenedId);
		SetVisualAndUiPin(NotOpenedId, notOpened);

		var notClosed = statusWord.GetBit(NotClosedId);
		SetVisualAndUiPin(NotClosedId, notClosed);

		var collision = statusWord.GetBit(CollisionId);
		SetVisualAndUiPin(CollisionId, collision);

		var usedByAutoMode = statusWord.GetBit(UsedByAutoModeId);
		SetVisualAndUiPin(UsedByAutoModeId, usedByAutoMode);

		var opened = statusWord.GetBit(OpenedId);
		SetVisualAndUiPin(OpenedId, opened);

		var openedSmoothly = statusWord.GetBit(SmoothlyOpenedId);
		SetVisualAndUiPin(SmoothlyOpenedId, openedSmoothly);

		var closed = statusWord.GetBit(ClosedId);
		SetVisualAndUiPin(ClosedId, closed);

		var openingClosing = statusWord.GetBit(OpeningClosingId);
		SetVisualAndUiPin(OpeningClosingId, openingClosing);

		var used = statusWord.GetBit(UsedId);
		SetVisualAndUiPin(UsedId, used);

		var manual = statusWord.GetBit(ManualId);
		SetVisualAndUiPin(ManualId, manual);

		var withoutSensors = statusWord.GetBit(WithoutSensorsId);
		SetVisualAndUiPin(WithoutSensorsId, withoutSensors);

		SetVisualAndUiPin(ForceCloseId, statusWord.GetBit(ForceCloseId));
		SetVisualAndUiPin(BlockClosingId, statusWord.GetBit(BlockClosingId));
		SetVisualAndUiPin(BlockOpeningId, statusWord.GetBit(BlockOpeningId));
		SetVisualAndUiPin(IsSmoothValveId, statusWord.GetBit(IsSmoothValveId));

		var commandWord = 0;
		var openCmd = GetVisualPin<bool>(OpenCmdId);
		var openSmoothlyCmd = GetVisualPin<bool>(OpenSmoothlyCmdId);
		var closeCmd = GetVisualPin<bool>(CloseCmdId);
		commandWord.SetBit(0, openCmd);
		commandWord.SetBit(1, openSmoothlyCmd);
		commandWord.SetBit(2, closeCmd);
		SetPinValue(CommandWordId, commandWord);

		if (openCmd && !_prevOpenCmd)
		{
			FireUserOpenEvent();
		}

		if (openSmoothlyCmd && !_prevOpenSmoothlyCmd)
		{
			FireUserOpenSmoothlyEvent();
		}

		if (closeCmd && !_prevCloseCmd)
		{
			FireUserCloseEvent();
		}

		_prevOpenCmd = openCmd;
		_prevOpenSmoothlyCmd = openSmoothlyCmd;
		_prevCloseCmd = closeCmd;

		var canRaisePositionErrors = used && !(manual && withoutSensors);
		_collisionEvent?.Update(collision && canRaisePositionErrors);
		_notOpenedEvent?.Update(notOpened && canRaisePositionErrors);
		_notClosedEvent?.Update(notClosed && canRaisePositionErrors);

		_openedEvent?.Update(opened && used);
		_openedSmoothlyEvent?.Update(openedSmoothly && used);
		_closedEvent?.Update(closed && used);
	}

	protected override OpcQuality GetConnectionQuality()
	{
		return GetPinQuality(StatusWordId);
	}

	public void FireUserOpenEvent()
	{
		if (!IsUsed())
		{
			return;
		}

		var splittedString = FullName.Split('.');
		var name = splittedString[^1];
		var message = $"Клапан {name}: оператор нажал кнопку Открыть";
		FireEvent(UserOpenEventId, message);
	}

	public void FireUserOpenSmoothlyEvent()
	{
		if (!IsUsed())
		{
			return;
		}

		var splittedString = FullName.Split('.');
		var name = splittedString[^1];
		var message = $"Клапан {name}: оператор нажал кнопку Плавно открыть";
		FireEvent(UserOpenSmoothlyEventId, message);
	}

	public void FireUserCloseEvent()
	{
		if (!IsUsed())
		{
			return;
		}

		var splittedString = FullName.Split('.');
		var name = splittedString[^1];
		var message = $"Клапан {name}: оператор нажал кнопку Закрыть";
		FireEvent(UserCloseEventId, message);
	}

	private bool IsUsed()
	{
		var statusWord = 0;
		if (GetPinQuality(StatusWordId) == OpcQuality.Ok)
		{
			statusWord = GetPinValue<int>(StatusWordId);
		}

		return statusWord.GetBit(UsedId);
	}
}
