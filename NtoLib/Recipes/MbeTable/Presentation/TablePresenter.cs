using System.Threading.Tasks;

using NtoLib.Recipes.MbeTable.Application;
using NtoLib.Recipes.MbeTable.Presentation.Abstractions;
using NtoLib.Recipes.MbeTable.Presentation.Commands;
using NtoLib.Recipes.MbeTable.Presentation.StateProviders;

namespace NtoLib.Recipes.MbeTable.Presentation;

public sealed class TablePresenter : ITablePresenter
{
    private readonly ITableView _view;
    private readonly IRecipeApplicationService _app;
    private readonly IRowExecutionStateProvider _rowStateProvider;

    private readonly LoadRecipeCommand _loadCmd;
    private readonly SaveRecipeCommand _saveCmd;
    private readonly SendRecipeCommand _sendCmd;
    private readonly ReceiveRecipeCommand _receiveCmd;
    private readonly RemoveStepCommand _removeStepCmd;
    private readonly AddStepCommand _addStepCmd;

    public TablePresenter(
        ITableView view,
        IRecipeApplicationService app,
        IRowExecutionStateProvider rowStateProvider,
        LoadRecipeCommand loadCmd,
        SaveRecipeCommand saveCmd,
        SendRecipeCommand sendCmd,
        ReceiveRecipeCommand receiveCmd,
        AddStepCommand addStepCmd,
        RemoveStepCommand removeStepCmd)
    {
        _view = view;
        _app = app;
        _rowStateProvider = rowStateProvider;
        _loadCmd = loadCmd;
        _saveCmd = saveCmd;
        _sendCmd = sendCmd;
        _receiveCmd = receiveCmd;
        _addStepCmd = addStepCmd;
        _removeStepCmd = removeStepCmd;

        _rowStateProvider.CurrentLineChanged += OnCurrentLineChanged;
    }

    public void Initialize()
    {
        _view.RowCount = _app.GetRowCount();
        _view.CellValueNeeded += OnCellValueNeeded;
        _view.CellValuePushed += OnCellValuePushed;

        _app.RecipeStructureChanged += OnRecipeStructureChanged;
        _app.StepDataChanged += row => _view.InvalidateRow(row);
    }

    public Task LoadRecipeAsync() => _loadCmd.ExecuteAsync();
    public Task SaveRecipeAsync() => _saveCmd.ExecuteAsync();
    public Task SendRecipeAsync() => _sendCmd.ExecuteAsync();
    public Task ReceiveRecipeAsync() => _receiveCmd.ExecuteAsync();

    public Task AddStepAfterCurrent()
    {
        var rowCount = _app.GetRowCount();
        var current = _view.CurrentRowIndex;
        var insert = current < 0 ? 0 : current + 1;
        if (insert > rowCount) insert = rowCount;
        return _addStepCmd.ExecuteAsync(insert);
    }

    public Task AddStepBeforeCurrent()
    {
        var rowCount = _app.GetRowCount();
        var current = _view.CurrentRowIndex;
        var insert = current < 0 ? 0 : current;
        if (insert > rowCount) insert = rowCount;
        return _addStepCmd.ExecuteAsync(insert);
    }

    public async Task RemoveCurrentStep()
    {
        var rowCount = _app.GetRowCount();
        var current = _view.CurrentRowIndex;
        if (current < 0 || current >= rowCount) return;
        await _removeStepCmd.ExecuteAsync(current).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _view.CellValueNeeded -= OnCellValueNeeded;
        _view.CellValuePushed -= OnCellValuePushed;
        _app.RecipeStructureChanged -= OnRecipeStructureChanged;
        _rowStateProvider.CurrentLineChanged -= OnCurrentLineChanged;
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
        var key = _view.GetColumnKey(e.ColumnIndex);
        if (key != null)
        {
            await _app.SetCellValueAsync(e.RowIndex, key, e.Value).ConfigureAwait(false);
        }
    }

    private void OnRecipeStructureChanged()
    {
        _view.RowCount = _app.GetRowCount();
        _view.Invalidate();
    }

    private void OnCurrentLineChanged(int oldIdx, int newIdx)
    {
        if (oldIdx >= 0 && oldIdx < _view.RowCount)
            _view.InvalidateRow(oldIdx);

        if (newIdx >= 0 && newIdx < _view.RowCount)
            _view.InvalidateRow(newIdx);

        if (newIdx >= 0)
            _view.EnsureRowVisible(newIdx);
    }
}