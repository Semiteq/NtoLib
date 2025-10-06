using System;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Presentation.Abstractions;

namespace NtoLib.Recipes.MbeTable.Presentation.Adapters;

public sealed class DataGridViewAdapter : ITableView, IDisposable
{
    private readonly DataGridView _grid;

    public DataGridViewAdapter(DataGridView grid)
    {
        _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        _grid.CellValueNeeded += OnCellValueNeededInternal;
        _grid.CellValuePushed += OnCellValuePushedInternal;
        _grid.CurrentCellChanged += OnCurrentCellChangedInternal;
    }

    public int RowCount
    {
        get => _grid.RowCount;
        set
        {
            if (_grid.IsDisposed) return;

            if (_grid.InvokeRequired)
            {
                try { _grid.BeginInvoke(new Action(() => _grid.RowCount = value)); }
                catch { }
            }
            else
            {
                _grid.RowCount = value;
            }
        }
    }

    public bool IsHandleCreated => _grid.IsHandleCreated;
    public bool IsDisposed => _grid.IsDisposed;

    public void Invalidate()
    {
        if (_grid.IsDisposed) return;

        if (_grid.InvokeRequired)
        {
            try { _grid.BeginInvoke(new Action(() => _grid.Invalidate())); }
            catch { }
        }
        else
        {
            _grid.Invalidate();
        }
    }

    public void InvalidateRow(int rowIndex)
    {
        if (_grid.IsDisposed) return;
        if (rowIndex < 0) return;

        if (_grid.InvokeRequired)
        {
            try { _grid.BeginInvoke(new Action(() => InvalidateRow(rowIndex))); }
            catch { }
            return;
        }

        if (rowIndex < _grid.RowCount)
            _grid.InvalidateRow(rowIndex);
    }

    public void InvalidateCell(int columnIndex, int rowIndex)
    {
        if (_grid.IsDisposed) return;
        if (rowIndex < 0 || columnIndex < 0) return;

        if (_grid.InvokeRequired)
        {
            try { _grid.BeginInvoke(new Action(() => InvalidateCell(columnIndex, rowIndex))); }
            catch { }
            return;
        }

        if (rowIndex < _grid.RowCount && columnIndex < _grid.ColumnCount)
            _grid.InvalidateCell(columnIndex, rowIndex);
    }

    public void EnsureRowVisible(int rowIndex)
    {
        if (_grid.IsDisposed) return;
        if (rowIndex < 0) return;

        if (_grid.InvokeRequired)
        {
            try { _grid.BeginInvoke(new Action(() => EnsureRowVisible(rowIndex))); }
            catch { }
            return;
        }

        if (!_grid.IsHandleCreated) return;
        if (rowIndex >= _grid.RowCount) return;

        try
        {
            var first = _grid.FirstDisplayedScrollingRowIndex;
            var visible = _grid.DisplayedRowCount(false);

            if (first < 0 || visible <= 0 || rowIndex < first || rowIndex >= first + visible)
                _grid.FirstDisplayedScrollingRowIndex = rowIndex;
        }
        catch
        {
            // Ignore transient failures when grid is in an invalid state (e.g., no rows yet displayed)
        }
    }

    public void BeginInvoke(Action action)
    {
        if (action == null) return;
        if (_grid.IsDisposed) return;

        if (_grid.InvokeRequired)
            _grid.BeginInvoke(action);
        else
            action();
    }

    public int CurrentRowIndex => _grid.CurrentCell?.RowIndex ?? -1;

    public ColumnIdentifier? GetColumnKey(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= _grid.ColumnCount)
            return null;

        var column = _grid.Columns[columnIndex];
        var key = !string.IsNullOrEmpty(column.Name)
            ? new ColumnIdentifier(column.Name)
            : null;

        return key;
    }

    public event EventHandler<CellValueEventArgs>? CellValueNeeded;
    public event EventHandler<CellValueEventArgs>? CellValuePushed;
    public event EventHandler? CurrentCellChanged;

    private void OnCellValueNeededInternal(object? sender, DataGridViewCellValueEventArgs e)
    {
        var args = new CellValueEventArgs(e.RowIndex, e.ColumnIndex);
        CellValueNeeded?.Invoke(this, args);
        e.Value = args.Value;
    }

    private void OnCellValuePushedInternal(object? sender, DataGridViewCellValueEventArgs e)
    {
        var args = new CellValueEventArgs(e.RowIndex, e.ColumnIndex, e.Value);
        CellValuePushed?.Invoke(this, args);
    }

    private void OnCurrentCellChangedInternal(object? sender, EventArgs e)
    {
        CurrentCellChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        try
        {
            _grid.CellValueNeeded -= OnCellValueNeededInternal;
            _grid.CellValuePushed -= OnCellValuePushedInternal;
            _grid.CurrentCellChanged -= OnCurrentCellChangedInternal;
        }
        catch { }
    }
}