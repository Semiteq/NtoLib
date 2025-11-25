using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
	private readonly IRecipeApplicationService _applicationService;
	private readonly CopyRowsCommand _copyCommand;
	private readonly CutRowsCommand _cutCommand;
	private readonly PasteRowsCommand _pasteCommand;
	private readonly DeleteRowsCommand _deleteCommand;
	private readonly InsertRowCommand _insertCommand;

	private ContextMenuStrip? _rowHeaderMenu;
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
		_applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
		_copyCommand = copyCommand ?? throw new ArgumentNullException(nameof(copyCommand));
		_cutCommand = cutCommand ?? throw new ArgumentNullException(nameof(cutCommand));
		_pasteCommand = pasteCommand ?? throw new ArgumentNullException(nameof(pasteCommand));
		_deleteCommand = deleteCommand ?? throw new ArgumentNullException(nameof(deleteCommand));
		_insertCommand = insertCommand ?? throw new ArgumentNullException(nameof(insertCommand));
	}

	public void Attach()
	{
		if (_attached)
			return;

		_table.KeyDown += OnTableKeyDown;
		_table.RowHeaderMouseClick += OnRowHeaderMouseClick;

		InitializeRowHeaderContextMenu();

		_attached = true;
	}

	public void Detach()
	{
		if (!_attached)
			return;

		try
		{ _table.KeyDown -= OnTableKeyDown; }
		catch
		{
			/* ignored */
		}

		try
		{ _table.RowHeaderMouseClick -= OnRowHeaderMouseClick; }
		catch
		{
			/* ignored */
		}

		if (_rowHeaderMenu != null)
		{
			try
			{ _rowHeaderMenu.Dispose(); }
			catch
			{
				/* ignored */
			}

			_rowHeaderMenu = null;
		}

		_attached = false;
	}

	public void Dispose()
	{
		Detach();
	}

	private async void OnTableKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Control && e.KeyCode == Keys.C)
		{
			await HandleCopyAsync(e).ConfigureAwait(false);
			return;
		}

		if (e.Control && e.KeyCode == Keys.X)
		{
			await HandleCutAsync(e).ConfigureAwait(false);
			return;
		}

		if (e.Control && e.KeyCode == Keys.V)
		{
			await HandlePasteAsync(e).ConfigureAwait(false);
			return;
		}

		if (e.Control && e.KeyCode == Keys.N)
		{
			await HandleInsertAsync(e).ConfigureAwait(false);
			return;
		}

		if (!e.Control && e.KeyCode == Keys.Delete)
		{
			await HandleDeleteAsync(e).ConfigureAwait(false);
		}
	}

	private async Task HandleCopyAsync(KeyEventArgs e)
	{
		var indices = GetSelectedRowIndices();
		if (indices.Count == 0)
			return;

		if (!_copyCommand.CanExecute())
			return;

		e.Handled = true;
		await _copyCommand.ExecuteAsync(indices).ConfigureAwait(false);
	}

	private async Task HandleCutAsync(KeyEventArgs e)
	{
		var indices = GetSelectedRowIndices();
		if (indices.Count == 0)
			return;

		if (!_cutCommand.CanExecute())
			return;

		e.Handled = true;
		await _cutCommand.ExecuteAsync(indices).ConfigureAwait(false);
	}

	private async Task HandleDeleteAsync(KeyEventArgs e)
	{
		var indices = GetSelectedRowIndices();
		if (indices.Count == 0)
			return;

		if (!_deleteCommand.CanExecute())
			return;

		e.Handled = true;
		await _deleteCommand.ExecuteAsync(indices).ConfigureAwait(false);
	}

	private async Task HandlePasteAsync(KeyEventArgs e)
	{
		if (_table.IsCurrentCellInEditMode)
			return;

		if (!_pasteCommand.CanExecute())
			return;

		var rowCount = _applicationService.GetRowCount();
		var targetIndex = GetInsertionIndexAfterSelection(rowCount);

		e.Handled = true;
		await _pasteCommand.ExecuteAsync(targetIndex).ConfigureAwait(false);
	}

	private async Task HandleInsertAsync(KeyEventArgs e)
	{
		if (!_insertCommand.CanExecute())
			return;

		var rowCount = _applicationService.GetRowCount();
		var insertIndex = GetInsertionIndexAfterSelection(rowCount);

		e.Handled = true;
		await _insertCommand.ExecuteAsync(insertIndex).ConfigureAwait(false);
	}

	private void OnRowHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
	{
		if (e.RowIndex < 0)
			return;

		if (e.Button == MouseButtons.Right)
		{
			ShowRowHeaderContextMenu(e.RowIndex, e.Location);
		}
	}

	private void InitializeRowHeaderContextMenu()
	{
		_rowHeaderMenu = new ContextMenuStrip();

		var copyItem = new ToolStripMenuItem("Copy rows", null, async (_, _) =>
		{
			var indices = GetSelectedRowIndices();
			if (indices.Count == 0)
				return;

			if (_copyCommand.CanExecute())
				await _copyCommand.ExecuteAsync(indices).ConfigureAwait(false);
		});

		var cutItem = new ToolStripMenuItem("Cut rows", null, async (_, _) =>
		{
			var indices = GetSelectedRowIndices();
			if (indices.Count == 0)
				return;

			if (_cutCommand.CanExecute())
				await _cutCommand.ExecuteAsync(indices).ConfigureAwait(false);
		});

		var pasteItem = new ToolStripMenuItem("Paste rows", null, async (_, _) =>
		{
			if (!_pasteCommand.CanExecute())
				return;

			var rowCount = _applicationService.GetRowCount();
			var targetIndex = GetInsertionIndexAfterSelection(rowCount);
			await _pasteCommand.ExecuteAsync(targetIndex).ConfigureAwait(false);
		});

		var deleteItem = new ToolStripMenuItem("Delete rows", null, async (_, _) =>
		{
			var indices = GetSelectedRowIndices();
			if (indices.Count == 0)
				return;

			if (_deleteCommand.CanExecute())
				await _deleteCommand.ExecuteAsync(indices).ConfigureAwait(false);
		});

		var newItem = new ToolStripMenuItem("New row", null, async (_, _) =>
		{
			if (!_insertCommand.CanExecute())
				return;

			var rowCount = _applicationService.GetRowCount();
			var insertIndex = GetInsertionIndexAfterSelection(rowCount);
			await _insertCommand.ExecuteAsync(insertIndex).ConfigureAwait(false);
		});

		_rowHeaderMenu.Items.AddRange(new ToolStripItem[]
		{
			copyItem,
			cutItem,
			pasteItem,
			new ToolStripSeparator(),
			deleteItem,
			new ToolStripSeparator(),
			newItem
		});
	}

	private void ShowRowHeaderContextMenu(int rowIndex, Point location)
	{
		if (_rowHeaderMenu == null)
			return;

		if (!_table.Rows[rowIndex].Selected)
		{
			_table.ClearSelection();
			_table.Rows[rowIndex].Selected = true;
		}

		var screenLocation = _table.PointToScreen(location);
		_rowHeaderMenu.Show(screenLocation);
	}

	private List<int> GetSelectedRowIndices()
	{
		if (_table.IsDisposed || _table.RowCount == 0)
			return new List<int>();

		var indices = _table.SelectedRows
			.Cast<DataGridViewRow>()
			.Select(row => row.Index)
			.Where(idx => idx >= 0 && idx < _table.RowCount)
			.Distinct()
			.OrderBy(idx => idx)
			.ToList();

		return indices;
	}

	private int GetInsertionIndexAfterSelection(int rowCount)
	{
		if (rowCount < 0)
			rowCount = 0;

		var selected = GetSelectedRowIndices();
		if (selected.Count > 0)
		{
			var index = selected[selected.Count - 1] + 1;
			if (index < 0)
				index = 0;
			if (index > rowCount)
				index = rowCount;
			return index;
		}

		var current = _table.CurrentCell?.RowIndex ?? -1;
		if (current >= 0)
		{
			var index = current + 1;
			if (index < 0)
				index = 0;
			if (index > rowCount)
				index = rowCount;
			return index;
		}

		return 0;
	}
}
