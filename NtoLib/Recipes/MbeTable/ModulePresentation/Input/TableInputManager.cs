using System;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModulePresentation.State;
using NtoLib.Recipes.MbeTable.Utilities;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Input;

/// <summary>
/// Handles keyboard shortcuts and row-header context menu for the recipe table.
/// Maps UI input (Ctrl+C/X/V/N/Delete, context menu) to clipboard and insert operations.
/// </summary>
public sealed class TableInputManager : IDisposable
{
	private readonly TableInputActions _actions;
	private readonly TableRowHeaderContextMenuService _rowHeaderMenuService;
	private readonly TableShortcutHandler _shortcutHandler;
	private readonly DataGridView _table;
	private bool _attached;

	private CtrlNHotkeyHook? _ctrlNHotkeyHook;

	public TableInputManager(
		DataGridView table,
		RecipeOperationService applicationService,
		BusyStateManager busyStateManager)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
		var selectionService = new TableSelectionService(_table);
		_actions = new TableInputActions(
			_table,
			applicationService,
			selectionService,
			busyStateManager);

		_shortcutHandler = new TableShortcutHandler(_table, _actions);
		_rowHeaderMenuService = new TableRowHeaderContextMenuService(_table, _actions);
	}

	public void Dispose()
	{
		Detach();
	}

	public void Attach()
	{
		if (_attached)
		{
			return;
		}

		_table.Disposed += OnTableDisposed;
		_shortcutHandler.Attach();
		_rowHeaderMenuService.Attach();
		EnsureCtrlNHotkeyHookInstalled();

		_attached = true;
	}

	public void Detach()
	{
		if (!_attached)
		{
			return;
		}

		SafeDisposal.RunAll(
			() => _table.Disposed -= OnTableDisposed,
			() => _shortcutHandler.Detach(),
			() => _rowHeaderMenuService.Detach(),
			RemoveCtrlNHotkeyHook);

		_attached = false;
	}

	private void OnTableDisposed(object? sender, EventArgs e)
	{
		Detach();
	}

	private void EnsureCtrlNHotkeyHookInstalled()
	{
		if (_ctrlNHotkeyHook != null)
		{
			return;
		}

		_ctrlNHotkeyHook = new CtrlNHotkeyHook(_table, OnCtrlNFromHotkeyHook);
	}

	private void RemoveCtrlNHotkeyHook()
	{
		if (_ctrlNHotkeyHook == null)
		{
			return;
		}

		SafeDisposal.TryDispose(_ctrlNHotkeyHook);
		_ctrlNHotkeyHook = null;
	}

	private void OnCtrlNFromHotkeyHook()
	{
		try
		{
			if (_table.IsDisposed)
			{
				return;
			}

			_table.BeginInvoke(new Action(async void () =>
			{
				try
				{
					await _actions.TryInsertAsync().ConfigureAwait(true);
				}
				catch
				{
					/* ignored */
				}
			}));
		}
		catch
		{
			/* ignored */
		}
	}
}
