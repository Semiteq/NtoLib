using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using FB;
using FB.VisualFB;

using InSAT.Library.Interop;

using NtoLib.Devices.Helpers;

namespace NtoLib.InputFields.TextBoxFloat;

[Serializable]
[ComVisible(true)]
[Guid("8A3C9E2F-D5B7-4A18-9F6C-E1A8B4D7C9F2")]
[CatID(CatIDs.CATID_OTHER)]
[DisplayName("Дробное поле")]
[VisualControls(typeof(TextBoxFloatControl))]
public class TextBoxFloatFB : VisualFBBase
{
	private const int InputFromScadaId = 10;
	private const int OutputToScadaId = 50;

	private const int LockFromScadaId = 15;

	private const int MaxValueId = 20;
	private const int MinValueId = 25;

	public const int OutputToControlId = 110;
	public const int InputFromControlId = 150;

	public const int LockToControl = 115;

	public const int MaxValueToControlId = 120;
	public const int MinValueToControlId = 125;

	public const int ValidationAboveMaxId = 200;
	public const int ValidationBelowMinId = 201;
	public const int ValidationParseErrorId = 202;

	private const int AboveMaxEventId = 5000;
	private const int BelowMinEventId = 5001;
	private const int ParseErrorEventId = 5002;

	private float _lastScadaInput;
	private float _lastControlOutput;

	[NonSerialized]
	private EventTrigger _aboveMaxEvent;

	[NonSerialized]
	private EventTrigger _belowMinEvent;

	[NonSerialized]
	private EventTrigger _parseErrorEvent;


	protected override void ToRuntime()
	{
		base.ToRuntime();

		var splittedString = FullName.Split('.');
		var name = splittedString[^1];
		var initialInactivity = TimeSpan.FromSeconds(5);

		var message = $"{name}: значение выше максимума";
		_aboveMaxEvent = new EventTrigger(this, AboveMaxEventId, message, initialInactivity);

		message = $"{name}: значение ниже минимума";
		_belowMinEvent = new EventTrigger(this, BelowMinEventId, message, initialInactivity);

		message = $"{name}: ошибка формата ввода";
		_parseErrorEvent = new EventTrigger(this, ParseErrorEventId, message, initialInactivity);

		var input = GetPinValue<float>(InputFromScadaId);
		_lastScadaInput = input;
		_lastControlOutput = input;
		VisualPins.SetPinValue(OutputToControlId, input);

		SetPinValue(OutputToScadaId, input);
	}

	protected override void UpdateData()
	{
		base.UpdateData();

		var scadaInput = GetPinValue<float>(InputFromScadaId);
		var controlOutput = VisualPins.GetPinValue<float>(InputFromControlId);

		if (scadaInput != _lastScadaInput)
		{
			_lastScadaInput = scadaInput;
			VisualPins.SetPinValue(OutputToControlId, scadaInput);
		}

		if (controlOutput != _lastControlOutput)
		{
			_lastControlOutput = controlOutput;
			SetPinValue(OutputToScadaId, controlOutput);
		}
		else if (scadaInput != controlOutput)
		{
			SetPinValue(OutputToScadaId, scadaInput);
		}

		var locked = GetPinValue<bool>(LockFromScadaId);
		VisualPins.SetPinValue(LockToControl, locked);

		var max = GetPinValue<float>(MaxValueId);
		VisualPins.SetPinValue(MaxValueToControlId, max);

		var min = GetPinValue<float>(MinValueId);
		VisualPins.SetPinValue(MinValueToControlId, min);

		var aboveMax = VisualPins.GetPinValue<bool>(ValidationAboveMaxId);
		_aboveMaxEvent.Update(aboveMax);

		var belowMin = VisualPins.GetPinValue<bool>(ValidationBelowMinId);
		_belowMinEvent.Update(belowMin);

		var parseError = VisualPins.GetPinValue<bool>(ValidationParseErrorId);
		_parseErrorEvent.Update(parseError);
	}
}
