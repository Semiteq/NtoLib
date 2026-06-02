using System;

using NtoLib.Recipes.MbeTable.ModulePresentation.Models;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.StateProviders;

/// <summary>
/// Abstraction over the per-row execution state consumed by the rendering pipeline.
/// The full FB resolves a PLC-driven implementation; the editor FB resolves a static
/// no-op implementation so the runtime poller and PLC pin accessors stay out of its graph.
/// </summary>
public interface IRowExecutionStateProvider : IDisposable
{
	RowExecutionState GetState(int rowIndex);

	event Action<int, int>? CurrentLineChanged;
}
