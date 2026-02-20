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
	private readonly double _debounceSeconds;
	private readonly float _precision;
	private DateTime _lastUiWriteTime;

	public ValueArbiter(float precision, double debounceSeconds)
	{
		_precision = precision;
		_debounceSeconds = debounceSeconds;
		_lastUiWriteTime = DateTime.MinValue;
	}

	public float OutputValue { get; private set; }

	public void Initialize(float value)
	{
		OutputValue = value;
		_lastUiWriteTime = DateTime.MinValue;
	}

	public void ApplyUiValue(float value)
	{
		if (Math.Abs(value - OutputValue) <= _precision)
		{
			return;
		}

		OutputValue = value;
		_lastUiWriteTime = DateTime.UtcNow;
	}

	public bool TryApplyPinValue(float value)
	{
		var pinChanged = Math.Abs(value - OutputValue) > _precision;
		if (!pinChanged)
		{
			return false;
		}

		var elapsed = DateTime.UtcNow.Subtract(_lastUiWriteTime).TotalSeconds;
		if (elapsed < _debounceSeconds)
		{
			return false;
		}

		OutputValue = value;

		return true;
	}
}
