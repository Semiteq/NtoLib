using System;
using System.Threading;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.State;

/// <summary>
/// Thread-safe manager controlling global UI busy flag.
/// </summary>
public sealed class BusyStateManager
{
	private int _counter;

	public bool IsBusy => Volatile.Read(ref _counter) > 0;

	public IDisposable Enter()
	{
		Interlocked.Increment(ref _counter);

		return new Scope(this);
	}

	private void Exit()
	{
		Interlocked.Decrement(ref _counter);
	}

	private sealed class Scope : IDisposable
	{
		private BusyStateManager? _owner;

		public Scope(BusyStateManager owner)
		{
			_owner = owner;
		}

		public void Dispose()
		{
			var owner = Interlocked.Exchange(ref _owner, null);
			owner?.Exit();
		}
	}
}
