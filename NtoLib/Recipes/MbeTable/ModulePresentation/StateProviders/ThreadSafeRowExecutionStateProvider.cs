using System;
using System.Threading;

using NtoLib.Recipes.MbeTable.ModuleInfrastructure.PinDataManager;
using NtoLib.Recipes.MbeTable.ModulePresentation.Models;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.StateProviders;

public sealed class ThreadSafeRowExecutionStateProvider : IDisposable
{
	private readonly RecipeRuntimeStatePoller _runtimeState;
	private int _currentStepIndex;

	public ThreadSafeRowExecutionStateProvider(RecipeRuntimeStatePoller runtimeState)
	{
		_runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
		_currentStepIndex = _runtimeState.Current.StepIndex;
		_runtimeState.StepPhaseChanged += OnPhaseChanged;
	}

	private int CurrentLineIndex => Volatile.Read(ref _currentStepIndex);

	public void Dispose()
	{
		try
		{
			_runtimeState.StepPhaseChanged -= OnPhaseChanged;
		}
		catch
		{
			/* ignored */
		}
	}

	public RowExecutionState GetState(int rowIndex)
	{
		var current = CurrentLineIndex;
		if (current < 0)
		{
			return RowExecutionState.Upcoming;
		}

		if (rowIndex < current)
		{
			return RowExecutionState.Passed;
		}

		if (rowIndex == current)
		{
			return RowExecutionState.Current;
		}

		return RowExecutionState.Upcoming;
	}

	public event Action<int, int>? CurrentLineChanged;

	private void OnPhaseChanged(StepPhase phase)
	{
		var old = Interlocked.Exchange(ref _currentStepIndex, phase.StepIndex);
		if (old != phase.StepIndex)
		{
			CurrentLineChanged?.Invoke(old, phase.StepIndex);
		}
	}
}
