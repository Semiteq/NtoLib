using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Core.Entities;
using NtoLib.Recipes.MbeTable.Presentation.Models;
using NtoLib.Recipes.MbeTable.Presentation.State;
using NtoLib.Recipes.MbeTable.Presentation.StateProviders;
using NtoLib.Recipes.MbeTable.Presentation.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Rendering;

public sealed class TableRenderCoordinator : ITableRenderCoordinator
{
    private readonly DataGridView _table;
    private readonly IRowExecutionStateProvider _rowExecutionStateProvider;
    private readonly ICellStateResolver _cellStateResolver;
    private readonly RecipeViewModel _recipeViewModel;
    private readonly IReadOnlyList<ColumnDefinition> _columns;
    private readonly ILogger _logger;
    private readonly IColorSchemeProvider _colorSchemeProvider;

    private bool _initialized;
    private bool _disposed;

    public TableRenderCoordinator(
        DataGridView table,
        IRowExecutionStateProvider rowExecutionStateProvider,
        ICellStateResolver cellStateResolver,
        RecipeViewModel recipeViewModel,
        IReadOnlyList<ColumnDefinition> columns,
        ILogger logger,
        IColorSchemeProvider colorSchemeProvider)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
        _rowExecutionStateProvider = rowExecutionStateProvider ?? throw new ArgumentNullException(nameof(rowExecutionStateProvider));
        _cellStateResolver = cellStateResolver ?? throw new ArgumentNullException(nameof(cellStateResolver));
        _recipeViewModel = recipeViewModel ?? throw new ArgumentNullException(nameof(recipeViewModel));
        _columns = columns ?? throw new ArgumentNullException(nameof(columns));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _colorSchemeProvider = colorSchemeProvider ?? throw new ArgumentNullException(nameof(colorSchemeProvider));
    }

    public void Initialize()
    {
        if (_initialized || _disposed)
            return;

        _table.CellFormatting += OnCellFormatting;
        _table.CellBeginEdit += OnCellBeginEdit;
        _table.CurrentCellDirtyStateChanged += OnCurrentCellDirtyStateChanged;
        _rowExecutionStateProvider.CurrentLineChanged += OnCurrentLineChanged;
        _colorSchemeProvider.Changed += OnColorSchemeChanged;

        _initialized = true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_initialized)
        {
            try { _table.CellFormatting -= OnCellFormatting; } catch { }
            try { _table.CellBeginEdit -= OnCellBeginEdit; } catch { }
            try { _table.CurrentCellDirtyStateChanged -= OnCurrentCellDirtyStateChanged; } catch { }
            try { _rowExecutionStateProvider.CurrentLineChanged -= OnCurrentLineChanged; } catch { }
            try { _colorSchemeProvider.Changed -= OnColorSchemeChanged; } catch { }
        }
    }

    private void OnColorSchemeChanged(ColorScheme obj)
    {
        try { _table.Invalidate(); }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to invalidate table on ColorScheme change");
        }
    }

    private void OnCellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

        try
        {
            var cell = _table.Rows[e.RowIndex].Cells[e.ColumnIndex];

            if (cell.Tag is CellVisualState visual && visual.IsReadOnly)
            {
                e.Cancel = true;
                return;
            }

            var state = _recipeViewModel.GetCellState(e.RowIndex, e.ColumnIndex);
            if (state == PropertyState.Disabled)
            {
                e.Cancel = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed during CellBeginEdit");
            e.Cancel = true;
        }
    }

    private void OnCurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (_table.IsCurrentCellDirty && _table.CurrentCell != null)
        {
            var rowIndex = _table.CurrentCell.RowIndex;
            var columnIndex = _table.CurrentCell.ColumnIndex;

            if (rowIndex >= 0 && rowIndex < _recipeViewModel.ViewModels.Count &&
                columnIndex >= 0 && columnIndex < _columns.Count)
            {
                var state = _recipeViewModel.GetCellState(rowIndex, columnIndex);
                if (state == PropertyState.Disabled)
                {
                    _table.CancelEdit();
                }
            }
        }
    }

    private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0
            || e.ColumnIndex < 0
            || e.ColumnIndex >= _columns.Count
            || e.RowIndex >= _recipeViewModel.ViewModels.Count)
            return;

        var visual = _cellStateResolver.Resolve(e.RowIndex, e.ColumnIndex, _recipeViewModel);

        e.CellStyle.Font = visual.Font;
        e.CellStyle.ForeColor = visual.ForeColor;
        e.CellStyle.BackColor = visual.BackColor;
        e.CellStyle.SelectionBackColor = visual.BackColor;
        e.CellStyle.SelectionForeColor = visual.ForeColor;

        var grid = (DataGridView)sender!;
        var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];

        // Publish visual to cell.Tag for custom paint (e.g., ComboBox cells)
        cell.Tag = visual;

        var currentReadOnly = cell.ReadOnly;
        if (currentReadOnly != visual.IsReadOnly)
        {
            try
            {
                cell.ReadOnly = visual.IsReadOnly;
            }
            catch (InvalidOperationException)
            {
                if (grid.IsHandleCreated && !grid.IsDisposed)
                {
                    grid.BeginInvoke(new Action(() =>
                    {
                        if (!grid.IsDisposed && e.RowIndex < grid.Rows.Count)
                        {
                            try
                            {
                                grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly = visual.IsReadOnly;
                            }
                            catch { }
                        }
                    }));
                }
            }
        }

        if (cell is DataGridViewComboBoxCell combo)
        {
            if (combo.DisplayStyle != visual.ComboDisplayStyle)
                combo.DisplayStyle = visual.ComboDisplayStyle;
        }
    }

    private void OnCurrentLineChanged(int oldIndex, int newIndex)
    {
        try
        {
            if (oldIndex >= 0 && oldIndex < _table.Rows.Count)
                _table.InvalidateRow(oldIndex);

            if (newIndex >= 0 && newIndex < _table.Rows.Count)
                _table.InvalidateRow(newIndex);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to invalidate rows on CurrentLineChanged");
        }
    }
}