using System;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModulePresentation.Style;
using NtoLib.Recipes.MbeTable.ServiceStatus;
using NtoLib.Recipes.MbeTable.Utilities;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Behavior;

public sealed class TableBehaviorManager : IDisposable
{
	private readonly ITableGridBehavior[] _behaviors;
	private readonly DataGridView _table;

	private bool _attached;
	private bool _disposed;

	public TableBehaviorManager(
		DataGridView table,
		StatusService? statusManager,
		ColorScheme colorScheme)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
		if (colorScheme == null)
		{
			throw new ArgumentNullException(nameof(colorScheme));
		}

		_behaviors = new ITableGridBehavior[]
		{
			new TableCellPaintingBehavior(_table, colorScheme), new TableRowNumberingBehavior(_table),
			new TableEditingControlBehavior(_table), new TableCellValidationBehavior(_table),
			new TableEditModeBehavior(_table), new TableDataErrorBehavior(_table, statusManager)
		};
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		SafeDisposal.RunAll(Detach);

		var disposeActions = new Action[_behaviors.Length];
		for (var i = 0; i < _behaviors.Length; i++)
		{
			var behavior = _behaviors[i];
			disposeActions[i] = () => behavior.Dispose();
		}

		SafeDisposal.RunAll(disposeActions);
	}

	public void Attach()
	{
		if (_disposed || _attached)
		{
			return;
		}

		_table.Disposed += OnTableDisposed;
		foreach (var behavior in _behaviors)
		{
			behavior.Attach();
		}

		_attached = true;
	}

	public void Detach()
	{
		if (!_attached)
		{
			return;
		}

		var actions = new Action[_behaviors.Length + 1];
		for (var i = 0; i < _behaviors.Length; i++)
		{
			var behavior = _behaviors[i];
			actions[i] = () => behavior.Detach();
		}

		actions[_behaviors.Length] = () => _table.Disposed -= OnTableDisposed;

		SafeDisposal.RunAll(actions);
		_attached = false;
	}

	private void OnTableDisposed(object? sender, EventArgs e)
	{
		SafeDisposal.RunAll(Detach);
	}
}
