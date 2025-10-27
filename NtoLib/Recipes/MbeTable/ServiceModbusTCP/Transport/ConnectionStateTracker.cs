using System;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;

internal sealed class ConnectionStateTracker
{
    private DateTime? _lastValidatedUtc;
    private DateTime? _lastOperationUtc;

    public DateTime? LastValidatedUtc => _lastValidatedUtc;
    public DateTime? LastOperationUtc => _lastOperationUtc;

    public bool IsStale(TimeSpan threshold)
    {
        if (_lastValidatedUtc == null)
            return true;

        if (threshold == TimeSpan.Zero)
            return true;

        return DateTime.UtcNow - _lastValidatedUtc.Value > threshold;
    }

    public void MarkValidated()
    {
        _lastValidatedUtc = DateTime.UtcNow;
    }

    public void MarkOperation()
    {
        _lastOperationUtc = DateTime.UtcNow;
    }

    public void Reset()
    {
        _lastValidatedUtc = null;
        _lastOperationUtc = null;
    }
}