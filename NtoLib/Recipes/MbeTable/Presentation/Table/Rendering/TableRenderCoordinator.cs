#nullable enable

using System;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Presentation.Table.State;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Rendering;

/// <summary>
/// Coordinates application of visual state (colors, fonts, interactivity) during CellFormatting.
/// </summary>
public sealed class TableRenderCoordinator : ITableRenderCoordinator
{
    private readonly DataGridView _table;
    private readonly IRowExecutionStateProvider _rowExecutionStateProvider;
    private readonly ICellStateResolver _cellStateResolver;
    private readonly RecipeViewModel _recipeViewModel;
    private readonly TableColumns _columns;
    private readonly ILogger _logger;
    private readonly IColorSchemeProvider _colorSchemeProvider;

    private bool _initialized;
    private bool _disposed;

    public TableRenderCoordinator(
        DataGridView table,
        IRowExecutionStateProvider rowExecutionStateProvider,
        ICellStateResolver cellStateResolver,
        RecipeViewModel recipeViewModel,
        TableColumns columns,
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
            try { _rowExecutionStateProvider.CurrentLineChanged -= OnCurrentLineChanged; } catch { }
            try { _colorSchemeProvider.Changed -= OnColorSchemeChanged; } catch { }
        }
    }

    private void OnColorSchemeChanged(ColorScheme obj)
    {
        try { _table.Invalidate(); }
        catch (Exception ex)
        {
            _logger.LogException(ex, "Failed to invalidate table on ColorScheme change");
        }
    }

    private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        if (e.RowIndex >= _recipeViewModel.ViewModels.Count)
            return;

        var vm = _recipeViewModel.ViewModels[e.RowIndex];
        var columnDef = _columns.GetColumnDefinition(e.ColumnIndex);
        var key = columnDef.Key;
        var visual = _cellStateResolver.Resolve(e.RowIndex, vm, key);

        e.CellStyle.Font = visual.Font;
        e.CellStyle.ForeColor = visual.ForeColor;
        e.CellStyle.BackColor = visual.BackColor;
        e.CellStyle.SelectionBackColor = visual.BackColor;
        e.CellStyle.SelectionForeColor = visual.ForeColor;

        var grid = (DataGridView)sender!;
        var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        if (cell.ReadOnly != visual.IsReadOnly)
            cell.ReadOnly = visual.IsReadOnly;

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
            _logger.LogException(ex, "Failed to invalidate rows on CurrentLineChanged");
        }
    }
}