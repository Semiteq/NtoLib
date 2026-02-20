using System;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;

internal sealed class ConnectionStateTracker
{
	private DateTime? _lastValidatedUtc;

	public DateTime? LastValidatedUtc => _lastValidatedUtc;
	public DateTime? LastOperationUtc { get; private set; }

	public bool IsStale(TimeSpan threshold)
	{
		if (_lastValidatedUtc == null)
		{
			return true;
		}

		if (threshold == TimeSpan.Zero)
		{
			return true;
		}

		return DateTime.UtcNow - _lastValidatedUtc.Value > threshold;
	}

	public void MarkValidated()
	{
		_lastValidatedUtc = DateTime.UtcNow;
	}

	public void MarkOperation()
	{
		LastOperationUtc = DateTime.UtcNow;
	}

	public void Reset()
	{
		_lastValidatedUtc = null;
		LastOperationUtc = null;
	}
}
