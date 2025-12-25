using System;
using System.Threading.Tasks;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModulePresentation.Commands;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Input;

internal sealed class TableInputActions
{
	private readonly DataGridView _table;
	private readonly IRecipeApplicationService _applicationService;
	private readonly TableSelectionService _selectionService;
	private readonly CopyRowsCommand _copyCommand;
	private readonly CutRowsCommand _cutCommand;
	private readonly PasteRowsCommand _pasteCommand;
	private readonly DeleteRowsCommand _deleteCommand;
	private readonly InsertRowCommand _insertCommand;

	public TableInputActions(
		DataGridView table,
		IRecipeApplicationService applicationService,
		TableSelectionService selectionService,
		CopyRowsCommand copyCommand,
		CutRowsCommand cutCommand,
		PasteRowsCommand pasteCommand,
		DeleteRowsCommand deleteCommand,
		InsertRowCommand insertCommand)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
		_applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
		_selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
		_copyCommand = copyCommand ?? throw new ArgumentNullException(nameof(copyCommand));
		_cutCommand = cutCommand ?? throw new ArgumentNullException(nameof(cutCommand));
		_pasteCommand = pasteCommand ?? throw new ArgumentNullException(nameof(pasteCommand));
		_deleteCommand = deleteCommand ?? throw new ArgumentNullException(nameof(deleteCommand));
		_insertCommand = insertCommand ?? throw new ArgumentNullException(nameof(insertCommand));
	}

	public async Task<bool> TryCopyAsync()
	{
		var indices = _selectionService.GetSelectedRowIndices();
		if (indices.Count == 0)
		{
			return false;
		}

		if (!_copyCommand.CanExecute())
		{
			return false;
		}

		await _copyCommand.ExecuteAsync(indices).ConfigureAwait(true);
		return true;
	}

	public bool CanCopy()
	{
		var indices = _selectionService.GetSelectedRowIndices();
		if (indices.Count == 0)
		{
			return false;
		}

		return _copyCommand.CanExecute();
	}

	public async Task<bool> TryCutAsync()
	{
		var indices = _selectionService.GetSelectedRowIndices();
		if (indices.Count == 0)
		{
			return false;
		}

		if (!_cutCommand.CanExecute())
		{
			return false;
		}

		await _cutCommand.ExecuteAsync(indices).ConfigureAwait(true);
		return true;
	}

	public bool CanCut()
	{
		var indices = _selectionService.GetSelectedRowIndices();
		if (indices.Count == 0)
		{
			return false;
		}

		return _cutCommand.CanExecute();
	}

	public async Task<bool> TryPasteAsync()
	{
		if (_table.IsCurrentCellInEditMode)
		{
			return false;
		}

		if (!_pasteCommand.CanExecute())
		{
			return false;
		}

		var rowCount = _applicationService.GetRowCount();
		var insertIndex = _selectionService.GetInsertionIndexAfterSelection(rowCount);

		await _pasteCommand.ExecuteAsync(insertIndex).ConfigureAwait(true);
		return true;
	}

	public bool CanPaste()
	{
		if (_table.IsCurrentCellInEditMode)
		{
			return false;
		}

		return _pasteCommand.CanExecute();
	}

	public async Task<bool> TryDeleteAsync()
	{
		var indices = _selectionService.GetSelectedRowIndices();
		if (indices.Count == 0)
		{
			return false;
		}

		if (!_deleteCommand.CanExecute())
		{
			return false;
		}

		await _deleteCommand.ExecuteAsync(indices).ConfigureAwait(true);
		return true;
	}

	public bool CanDelete()
	{
		var indices = _selectionService.GetSelectedRowIndices();
		if (indices.Count == 0)
		{
			return false;
		}

		return _deleteCommand.CanExecute();
	}

	public async Task<bool> TryInsertAsync()
	{
		if (!_insertCommand.CanExecute())
		{
			return false;
		}

		var rowCount = _applicationService.GetRowCount();
		var insertIndex = _selectionService.GetInsertionIndexAfterSelection(rowCount);

		await _insertCommand.ExecuteAsync(insertIndex).ConfigureAwait(true);
		return true;
	}

	public bool CanInsert()
	{
		return _insertCommand.CanExecute();
	}
}
