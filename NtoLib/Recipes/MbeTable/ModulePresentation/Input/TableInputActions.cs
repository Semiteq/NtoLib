using System;
using System.Threading.Tasks;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModulePresentation.State;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Input;

internal sealed class TableInputActions
{
	private readonly DataGridView _table;
	private readonly IRecipeApplicationService _applicationService;
	private readonly TableSelectionService _selectionService;
	private readonly IBusyStateManager _busy;

	public TableInputActions(
		DataGridView table,
		IRecipeApplicationService applicationService,
		TableSelectionService selectionService,
		IBusyStateManager busy)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
		_applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
		_selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
		_busy = busy ?? throw new ArgumentNullException(nameof(busy));
	}

	public async Task<bool> TryCopyAsync()
	{
		var indices = _selectionService.GetSelectedRowIndices();
		if (indices.Count == 0)
			return false;

		if (_busy.IsBusy)
			return false;

		await _applicationService.CopyRowsAsync(indices).ConfigureAwait(true);
		return true;
	}

	public bool CanCopy()
	{
		var indices = _selectionService.GetSelectedRowIndices();
		if (indices.Count == 0)
			return false;

		return !_busy.IsBusy;
	}

	public async Task<bool> TryCutAsync()
	{
		var indices = _selectionService.GetSelectedRowIndices();
		if (indices.Count == 0)
			return false;

		if (_busy.IsBusy)
			return false;

		await _applicationService.CutRowsAsync(indices).ConfigureAwait(true);
		return true;
	}

	public bool CanCut()
	{
		var indices = _selectionService.GetSelectedRowIndices();
		if (indices.Count == 0)
			return false;

		return !_busy.IsBusy;
	}

	public async Task<bool> TryPasteAsync()
	{
		if (_table.IsCurrentCellInEditMode)
			return false;

		if (_busy.IsBusy)
			return false;

		var rowCount = _applicationService.GetRowCount();
		var insertIndex = _selectionService.GetInsertionIndexAfterSelection(rowCount);

		await _applicationService.PasteRowsAsync(insertIndex).ConfigureAwait(true);
		return true;
	}

	public bool CanPaste()
	{
		if (_table.IsCurrentCellInEditMode)
			return false;

		return !_busy.IsBusy;
	}

	public async Task<bool> TryDeleteAsync()
	{
		var indices = _selectionService.GetSelectedRowIndices();
		if (indices.Count == 0)
			return false;

		if (_busy.IsBusy)
			return false;

		await _applicationService.DeleteRowsAsync(indices).ConfigureAwait(true);
		return true;
	}

	public bool CanDelete()
	{
		var indices = _selectionService.GetSelectedRowIndices();
		if (indices.Count == 0)
			return false;

		return !_busy.IsBusy;
	}

	public async Task<bool> TryInsertAsync()
	{
		if (_busy.IsBusy)
			return false;

		var rowCount = _applicationService.GetRowCount();
		var insertIndex = _selectionService.GetInsertionIndexAfterSelection(rowCount);

		_applicationService.AddStep(insertIndex);
		return true;
	}

	public bool CanInsert()
	{
		return !_busy.IsBusy;
	}
}
