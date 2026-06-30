using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModulePresentation.Adapters;
using NtoLib.Recipes.MbeTable.ModulePresentation.State;
using NtoLib.Recipes.MbeTable.ModulePresentation.StateProviders;
using NtoLib.Recipes.MbeTable.ServiceStatus;

namespace NtoLib.Recipes.MbeTable.ModulePresentation;

public sealed class TablePresenter : IDisposable
{
	private readonly RecipeOperationService _app;
	private readonly BusyStateManager _busy;
	private readonly IStatusSink _fileSink;
	private readonly OpenFileDialog _openDialog;
	private readonly IRowExecutionStateProvider _rowStateProvider;
	private readonly SaveFileDialog _saveDialog;
	private readonly ITableView _view;

	public TablePresenter(
		ITableView view,
		RecipeOperationService app,
		IRowExecutionStateProvider rowStateProvider,
		BusyStateManager busy,
		OpenFileDialog openDialog,
		SaveFileDialog saveDialog,
		IStatusSink fileSink)
	{
		_view = view;
		_app = app;
		_rowStateProvider = rowStateProvider;
		_busy = busy;
		_openDialog = openDialog;
		_saveDialog = saveDialog;
		_fileSink = fileSink;

		_rowStateProvider.CurrentLineChanged += OnCurrentLineChanged;
	}

	public void Dispose()
	{
		_view.CellValueNeeded -= OnCellValueNeeded;
		_view.CellValuePushed -= OnCellValuePushed;
		_app.RecipeStructureChanged -= OnRecipeStructureChanged;
		_rowStateProvider.CurrentLineChanged -= OnCurrentLineChanged;
		(_fileSink as IDisposable)?.Dispose();
	}

	public void Initialize()
	{
		_view.RowCount = _app.GetRowCount();
		_view.CellValueNeeded += OnCellValueNeeded;
		_view.CellValuePushed += OnCellValuePushed;

		_app.RecipeStructureChanged += OnRecipeStructureChanged;
		_app.StepDataChanged += row => _view.InvalidateRow(row);

		ShowCurrentFile(null);
	}

	private void ShowCurrentFile(string? fullPath)
	{
		var text = fullPath is null ? "Несохранённый рецепт" : "Сейчас открыт: " + fullPath;
		_fileSink.Write(text, StatusKind.None);
	}

	public async Task LoadRecipeAsync()
	{
		if (_openDialog.ShowDialog() != DialogResult.OK)
		{
			return;
		}

		var path = _openDialog.FileName;
		if (!File.Exists(path))
		{
			return;
		}

		using (_busy.Enter())
		{
			var result = await _app.LoadRecipeAsync(path).ConfigureAwait(false);
			if (result.IsSuccess)
			{
				ShowCurrentFile(path);
			}
		}
	}

	public async Task SaveRecipeAsync()
	{
		if (_saveDialog.ShowDialog() != DialogResult.OK)
		{
			return;
		}

		using (_busy.Enter())
		{
			var result = await _app.SaveRecipeAsync(_saveDialog.FileName).ConfigureAwait(false);
			if (result.IsSuccess)
			{
				ShowCurrentFile(_saveDialog.FileName);
			}
		}
	}

	public async Task SendRecipeAsync()
	{
		using (_busy.Enter())
		{
			await _app.SendRecipeAsync().ConfigureAwait(false);
		}
	}

	public async Task ReceiveRecipeAsync()
	{
		using (_busy.Enter())
		{
			var result = await _app.ReceiveRecipeAsync().ConfigureAwait(false);
			if (result.IsSuccess)
			{
				ShowCurrentFile(null);
			}
		}
	}

	public Task AddStepAfterCurrent()
	{
		var rowCount = _app.GetRowCount();
		var current = _view.CurrentRowIndex;
		var insert = current < 0 ? 0 : current + 1;
		if (insert > rowCount)
		{
			insert = rowCount;
		}

		_app.AddStep(insert);

		return Task.CompletedTask;
	}

	public Task AddStepBeforeCurrent()
	{
		var rowCount = _app.GetRowCount();
		var current = _view.CurrentRowIndex;
		var insert = current < 0 ? 0 : current;
		if (insert > rowCount)
		{
			insert = rowCount;
		}

		_app.AddStep(insert);

		return Task.CompletedTask;
	}

	public Task RemoveCurrentStep()
	{
		var rowCount = _app.GetRowCount();
		var current = _view.CurrentRowIndex;
		if (current < 0 || current >= rowCount)
		{
			return Task.CompletedTask;
		}

		_app.RemoveStep(current);

		return Task.CompletedTask;
	}

	private void OnCellValueNeeded(object? _, CellValueEventArgs e)
	{
		var totalRows = _app.GetRowCount();
		if (e.RowIndex < 0 || e.RowIndex >= totalRows)
		{
			e.Value = null;

			return;
		}

		var result = _app.ViewModel.GetCellValue(e.RowIndex, e.ColumnIndex);
		e.Value = result.IsSuccess ? result.Value : null;
	}

	private async void OnCellValuePushed(object? sender, CellValueEventArgs e)
	{
		if (e.Value == null)
		{
			return;
		}

		var key = _view.GetColumnKey(e.ColumnIndex);
		if (key == null)
		{
			return;
		}

		var currentValueResult = _app.ViewModel.GetCellValue(e.RowIndex, e.ColumnIndex);
		if (currentValueResult.IsSuccess)
		{
			var currentValue = currentValueResult.Value;
			if (ValuesAreEqual(currentValue, e.Value))
			{
				return;
			}
		}

		await _app.SetCellValueAsync(e.RowIndex, key, e.Value).ConfigureAwait(false);
	}

	private static bool ValuesAreEqual(object? left, object? right)
	{
		if (ReferenceEquals(left, right))
		{
			return true;
		}

		if (left == null || right == null)
		{
			return false;
		}

		if (left.Equals(right))
		{
			return true;
		}

		var leftString = left.ToString();
		var rightString = right.ToString();

		return string.Equals(leftString, rightString, StringComparison.Ordinal);
	}

	private void OnRecipeStructureChanged(StructureChange change)
	{
		_view.RowCount = _app.GetRowCount();
		_view.Invalidate();
	}

	private void OnCurrentLineChanged(int oldIdx, int newIdx)
	{
		if (oldIdx >= 0 && oldIdx < _view.RowCount)
		{
			_view.InvalidateRow(oldIdx);
		}

		if (newIdx >= 0 && newIdx < _view.RowCount)
		{
			_view.InvalidateRow(newIdx);
		}

		if (newIdx >= 0)
		{
			_view.EnsureRowVisible(newIdx);
		}
	}
}
