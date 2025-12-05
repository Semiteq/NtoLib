using System;
using System.Threading;

using NtoLib.MbeTable.ModuleInfrastructure.PinDataManager;
using NtoLib.MbeTable.ModulePresentation.Models;

namespace NtoLib.MbeTable.ModulePresentation.StateProviders;

/// <summary>
/// Thread-safe re-implementation of <see cref="RowExecutionStateProvider"/> based on Interlocked,
/// eliminates race conditions between PLC polling thread and UI thread.
/// </summary>
public sealed class ThreadSafeRowExecutionStateProvider : IRowExecutionStateProvider
{
	private readonly IRecipeRuntimeState _runtimeState;
	private int _currentStepIndex;

	public ThreadSafeRowExecutionStateProvider(IRecipeRuntimeState runtimeState)
	{
		_runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
		_currentStepIndex = _runtimeState.Current.StepIndex;
		_runtimeState.StepPhaseChanged += OnPhaseChanged;
	}

	public int CurrentLineIndex => Volatile.Read(ref _currentStepIndex);

	public RowExecutionState GetState(int rowIndex)
	{
		var current = CurrentLineIndex;
		if (current < 0)
			return RowExecutionState.Upcoming;
		if (rowIndex < current)
			return RowExecutionState.Passed;
		if (rowIndex == current)
			return RowExecutionState.Current;
		return RowExecutionState.Upcoming;
	}

	public event Action<int, int>? CurrentLineChanged;

	private void OnPhaseChanged(StepPhase phase)
	{
		var old = Interlocked.Exchange(ref _currentStepIndex, phase.StepIndex);
		if (old != phase.StepIndex)
			CurrentLineChanged?.Invoke(old, phase.StepIndex);
	}

	public void Dispose()
	{
		try
		{ _runtimeState.StepPhaseChanged -= OnPhaseChanged; }
		catch
		{
			/* ignored */
		}
	}
}
