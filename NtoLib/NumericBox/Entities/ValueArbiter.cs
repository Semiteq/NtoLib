using System;

namespace NtoLib.NumericBox.Entities;

/// <summary>
/// Arbitrates between two value sources (UI input and external pin input)
/// to produce a single output value. After a UI write, pin updates are
/// suppressed for a debounce window to prevent the feedback loop from
/// overwriting the user's value before the external system processes it.
/// </summary>
[Serializable]
public class ValueArbiter
{
	private readonly float _precision;
	private readonly double _debounceSeconds;

	private float _outputValue;
	private DateTime _lastUiWriteTime;

	public ValueArbiter(float precision, double debounceSeconds)
	{
		_precision = precision;
		_debounceSeconds = debounceSeconds;
		_lastUiWriteTime = DateTime.MinValue;
	}

	public float OutputValue => _outputValue;

	public void Initialize(float value)
	{
		_outputValue = value;
		_lastUiWriteTime = DateTime.MinValue;
	}

	public void ApplyUiValue(float value)
	{
		if (Math.Abs(value - _outputValue) <= _precision)
		{
			return;
		}

		_outputValue = value;
		_lastUiWriteTime = DateTime.UtcNow;
	}

	public bool TryApplyPinValue(float value)
	{
		var pinChanged = Math.Abs(value - _outputValue) > _precision;
		if (!pinChanged)
		{
			return false;
		}

		var elapsed = DateTime.UtcNow.Subtract(_lastUiWriteTime).TotalSeconds;
		if (elapsed < _debounceSeconds)
		{
			return false;
		}

		_outputValue = value;
		return true;
	}
}
