using System;
using System.Threading.Tasks;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModulePresentation.Commands;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Input;

/// <summary>
/// Handles keyboard shortcuts and row-header context menu for the recipe table.
/// Maps UI input (Ctrl+C/X/V/N/Delete, context menu) to clipboard and insert commands.
/// </summary>
public sealed class TableInputManager : IDisposable
{
	private readonly DataGridView _table;
	private readonly TableInputActions _actions;
	private readonly TableShortcutHandler _shortcutHandler;
	private readonly TableRowHeaderContextMenuService _rowHeaderMenuService;

	private CtrlNHotkeyHook? _ctrlNHotkeyHook;
	private bool _attached;

	public TableInputManager(
		DataGridView table,
		IRecipeApplicationService applicationService,
		CopyRowsCommand copyCommand,
		CutRowsCommand cutCommand,
		PasteRowsCommand pasteCommand,
		DeleteRowsCommand deleteCommand,
		InsertRowCommand insertCommand)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
		var selectionService = new TableSelectionService(_table);
		_actions = new TableInputActions(
			_table,
			applicationService,
			selectionService,
			copyCommand,
			cutCommand,
			pasteCommand,
			deleteCommand,
			insertCommand);

		_shortcutHandler = new TableShortcutHandler(_table, _actions);
		_rowHeaderMenuService = new TableRowHeaderContextMenuService(_table, _actions);
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

		try
		{
			_table.Disposed -= OnTableDisposed;
		}
		catch
		{
			/* ignored */
		}

		try
		{
			_shortcutHandler.Detach();
		}
		catch
		{
			/* ignored */
		}

		try
		{
			_rowHeaderMenuService.Detach();
		}
		catch
		{
			/* ignored */
		}

		RemoveCtrlNHotkeyHook();

		_attached = false;
	}

	private void OnTableDisposed(object? sender, EventArgs e)
	{
		try
		{
			Detach();
		}
		catch
		{
			/* ignored */
		}
	}

	public void Dispose()
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

		try
		{
			_ctrlNHotkeyHook.Dispose();
		}
		catch
		{
			/* ignored */
		}

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

			_table.BeginInvoke(new Action(async () =>
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
