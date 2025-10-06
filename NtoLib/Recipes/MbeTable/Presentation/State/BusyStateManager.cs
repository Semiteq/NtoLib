using System;
using System.Threading;
using NtoLib.Recipes.MbeTable.Application.State;

namespace NtoLib.Recipes.MbeTable.Presentation.State;

/// <summary>
/// Thread-safe manager controlling global UI busy flag via <see cref="Enter"/> / scope pattern.
/// </summary>
public sealed class BusyStateManager : IBusyStateManager
{
    private int _counter;     // Number of nested busy scopes
    private OperationKind? _currentOperation;

    public bool IsBusy => Volatile.Read(ref _counter) > 0;

    public event Action<bool>? BusyStateChanged;

    /// <inheritdoc />
    public IDisposable Enter(OperationKind operation)
    {
        if (Interlocked.Increment(ref _counter) == 1)
        {
            _currentOperation = operation;
            BusyStateChanged?.Invoke(true);
        }

        return new Scope(this);
    }

    private void Exit()
    {
        if (Interlocked.Decrement(ref _counter) == 0)
        {
            _currentOperation = null;
            BusyStateChanged?.Invoke(false);
        }
    }

    private sealed class Scope : IDisposable
    {
        private BusyStateManager? _owner;
        public Scope(BusyStateManager owner) => _owner = owner;

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null);
            owner?.Exit();
        }
    }
}