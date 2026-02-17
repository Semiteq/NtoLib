using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using FB;
using FB.VisualFB;

using FluentResults;

using InSAT.Library.Interop;

using MasterSCADA.Hlp;

using MasterSCADALib;

using NtoLib.Devices.Helpers;
using NtoLib.NumericBox.Entities;

namespace NtoLib.NumericBox;

[Serializable]
[ComVisible(true)]
[Guid("2E7D90D7-D24A-4A23-AD06-D797F1721E79")]
[CatID(CatIDs.CATID_OTHER)]
[DisplayName("Поле ввода")]
[VisualControls(typeof(NumericBoxControl))]
public class NumericBoxFB : VisualFBBase
{
	private const int PinValueId = 10;
	private const int PoutValueId = 50;
	private const int PinLockId = 15;
	private const int MaxValueId = 20;
	private const int MinValueId = 25;

	private const int AboveMaxEventId = 5000;
	private const int BelowMinEventId = 5001;
	private const int LimitsInconsistentEventId = 5003;

	private const float DefaultPrecision = 0.001f;
	private const double DefaultDebounceSeconds = 1.0;
	private static readonly TimeSpan _initialInactivity = TimeSpan.FromSeconds(5);

	private readonly ValueArbiter _arbiter = new(DefaultPrecision, DefaultDebounceSeconds);

	[NonSerialized]
	private EventTrigger? _aboveMaxEvent;

	[NonSerialized]
	private EventTrigger? _belowMinEvent;

	[NonSerialized]
	private EventTrigger? _limitsInconsistentEvent;

	[Browsable(false)]
	public float OutputValue => _arbiter.OutputValue;

	[Browsable(false)]
	public bool IsLocked => GetPinValue<bool>(PinLockId);

	public void ApplyUiValue(float value)
	{
		_arbiter.ApplyUiValue(value);
		SetPinValue(PoutValueId, _arbiter.OutputValue);
	}

	public Result ValidateValue(float value, string displayFormat)
	{
		var min = GetConnectedLimit(MinValueId);
		var max = GetConnectedLimit(MaxValueId);

		if (min.HasValue && value < min.Value)
		{
			var limitText = min.Value.ToString(displayFormat);
			return new Error($"Значение должно быть не менее {limitText}");
		}

		if (max.HasValue && value > max.Value)
		{
			var limitText = max.Value.ToString(displayFormat);
			return new Error($"Значение должно быть не более {limitText}");
		}

		ApplyUiValue(value);
		return Result.Ok();
	}

	protected override void ToRuntime()
	{
		base.ToRuntime();

		var initialPinValue = GetPinValue<float>(PinValueId);
		_arbiter.Initialize(initialPinValue);
		SetPinValue(PoutValueId, initialPinValue);

		InitializeEventTriggers();
	}

	protected override void UpdateData()
	{
		base.UpdateData();

		var pinValue = GetPinValue<float>(PinValueId);
		if (_arbiter.TryApplyPinValue(pinValue))
		{
			SetPinValue(PoutValueId, _arbiter.OutputValue);
		}

		UpdateEventStates();
	}

	private void InitializeEventTriggers()
	{
		var name = FullName.Split('.')[^1];

		_aboveMaxEvent = new EventTrigger(this,
			AboveMaxEventId,
			$"{name}: значение выше максимума",
			_initialInactivity);

		_belowMinEvent = new EventTrigger(this,
			BelowMinEventId,
			$"{name}: значение ниже минимума",
			_initialInactivity);

		_limitsInconsistentEvent = new EventTrigger(this,
			LimitsInconsistentEventId,
			$"{name}: минимум превышает максимум",
			_initialInactivity);
	}

	private void UpdateEventStates()
	{
		var output = _arbiter.OutputValue;
		var min = GetConnectedLimit(MinValueId);
		var max = GetConnectedLimit(MaxValueId);

		var isAboveMax = max.HasValue && output > max.Value;
		var isBelowMin = min.HasValue && output < min.Value;
		var areLimitsInconsistent = min.HasValue && max.HasValue && min.Value > max.Value;

		_aboveMaxEvent?.Update(isAboveMax);
		_belowMinEvent?.Update(isBelowMin);
		_limitsInconsistentEvent?.Update(areLimitsInconsistent);
	}

	private float? GetConnectedLimit(int pinId)
	{
		if (!HasLinkConnected(pinId))
		{
			return null;
		}

		return GetPinValue<float>(pinId);
	}

	private bool HasLinkConnected(int pinId)
	{
		if (TreeItemHlp == null)
		{
			return false;
		}

		var pinDef = PinByID(pinId);
		if (pinDef == null)
		{
			return false;
		}

		if (TreeItemHlp.GetChild("RangeLimits." + pinDef.Name) is not ITreePinHlp treePinHlp)
		{
			return false;
		}

		var connections = treePinHlp.GetConnections(EConnectionTypeMask.ctGeneric);
		return connections.Length > 0;
	}
}
