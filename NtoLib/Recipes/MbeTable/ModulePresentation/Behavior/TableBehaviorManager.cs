using System;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModulePresentation.Style;
using NtoLib.Recipes.MbeTable.ServiceStatus;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Behavior;

public sealed class TableBehaviorManager : IDisposable
{
	private readonly DataGridView _table;
	private readonly ITableGridBehavior[] _behaviors;

	private bool _attached;
	private bool _disposed;

	public TableBehaviorManager(
		DataGridView table,
		IStatusService? statusManager,
		ColorScheme colorScheme)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
		if (colorScheme == null)
		{
			throw new ArgumentNullException(nameof(colorScheme));
		}

		_behaviors = new ITableGridBehavior[]
		{
			new TableCellPaintingBehavior(_table, colorScheme),
			new TableRowNumberingBehavior(_table),
			new TableEditingControlBehavior(_table),
			new TableCellValidationBehavior(_table),
			new TableEditModeBehavior(_table),
			new TableDataErrorBehavior(_table, statusManager)
		};
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

		foreach (var behavior in _behaviors)
		{
			try
			{
				behavior.Detach();
			}
			catch
			{
				// ignored
			}
		}

		try
		{
			_table.Disposed -= OnTableDisposed;
		}
		catch
		{
			// ignored
		}

		_attached = false;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		try
		{
			Detach();
		}
		catch
		{
			// ignored
		}

		foreach (var behavior in _behaviors)
		{
			try
			{
				behavior.Dispose();
			}
			catch
			{
				// ignored
			}
		}
	}

	private void OnTableDisposed(object? sender, EventArgs e)
	{
		try
		{
			Detach();
		}
		catch
		{
			// ignored
		}
	}
}
