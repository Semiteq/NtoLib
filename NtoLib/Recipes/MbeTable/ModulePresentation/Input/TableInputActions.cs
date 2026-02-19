using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModulePresentation.State;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Input;

internal sealed class TableInputActions
{
	private readonly DataGridView _table;
	private readonly RecipeOperationService _applicationService;
	private readonly TableSelectionService _selectionService;
	private readonly BusyStateManager _busy;

	public TableInputActions(
		DataGridView table,
		RecipeOperationService applicationService,
		TableSelectionService selectionService,
		BusyStateManager busy)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
		_applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
		_selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
		_busy = busy ?? throw new ArgumentNullException(nameof(busy));
	}

	public bool CanCopy() => CanOperateOnSelection();
	public bool CanCut() => CanOperateOnSelection();
	public bool CanDelete() => CanOperateOnSelection();
	public bool CanPaste() => !_table.IsCurrentCellInEditMode && !_busy.IsBusy;
	public bool CanInsert() => !_busy.IsBusy;

	public Task<bool> TryCopyAsync() =>
		TryOperateOnSelectionAsync(indices => _applicationService.CopyRowsAsync(indices));

	public Task<bool> TryCutAsync() =>
		TryOperateOnSelectionAsync(indices => _applicationService.CutRowsAsync(indices));

	public Task<bool> TryDeleteAsync() =>
		TryOperateOnSelectionAsync(indices => _applicationService.DeleteRowsAsync(indices));

	public async Task<bool> TryPasteAsync()
	{
		if (!CanPaste())
		{
			return false;
		}

		var rowCount = _applicationService.GetRowCount();
		var insertIndex = _selectionService.GetInsertionIndexAfterSelection(rowCount);

		await _applicationService.PasteRowsAsync(insertIndex).ConfigureAwait(true);
		return true;
	}

	public Task<bool> TryInsertAsync()
	{
		if (_busy.IsBusy)
		{
			return Task.FromResult(false);
		}

		var rowCount = _applicationService.GetRowCount();
		var insertIndex = _selectionService.GetInsertionIndexAfterSelection(rowCount);

		_applicationService.AddStep(insertIndex);
		return Task.FromResult(true);
	}

	private bool CanOperateOnSelection() =>
		_selectionService.GetSelectedRowIndices().Count > 0 && !_busy.IsBusy;

	private async Task<bool> TryOperateOnSelectionAsync(Func<IReadOnlyList<int>, Task> operation)
	{
		var indices = _selectionService.GetSelectedRowIndices();
		if (indices.Count == 0 || _busy.IsBusy)
		{
			return false;
		}

		await operation(indices).ConfigureAwait(true);
		return true;
	}
}
