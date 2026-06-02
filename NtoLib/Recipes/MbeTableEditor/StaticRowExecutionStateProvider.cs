using System;

using NtoLib.Recipes.MbeTable.ModulePresentation.Models;
using NtoLib.Recipes.MbeTable.ModulePresentation.StateProviders;

namespace NtoLib.Recipes.MbeTableEditor;

/// <summary>
/// Editor-only row execution state provider. Reports every row as
/// <see cref="RowExecutionState.Upcoming"/> and never raises change events, so the
/// PLC runtime poller and pin accessors are kept out of the editor DI graph.
/// </summary>
public sealed class StaticRowExecutionStateProvider : IRowExecutionStateProvider
{
	public RowExecutionState GetState(int rowIndex)
	{
		return RowExecutionState.Upcoming;
	}

	public event Action<int, int>? CurrentLineChanged
	{
		add { }
		remove { }
	}

	public void Dispose()
	{
	}
}
