using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModulePresentation.Cells;
using NtoLib.Recipes.MbeTable.ModulePresentation.Models;
using NtoLib.Recipes.MbeTable.ModulePresentation.State;
using NtoLib.Recipes.MbeTable.ModulePresentation.StateProviders;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Rendering;

public sealed class TableRenderCoordinator : ITableRenderCoordinator
{
    private readonly DataGridView _table;
    private readonly IRowExecutionStateProvider _rowExecutionStateProvider;
    private readonly ICellStateResolver _cellStateResolver;
    private readonly RecipeViewModel _recipeViewModel;
    private readonly IReadOnlyList<ColumnDefinition> _columns;
    private readonly ILogger<TableRenderCoordinator> _logger;
    private readonly IColorSchemeProvider _colorSchemeProvider;

    private bool _initialized;
    private bool _disposed;

    public TableRenderCoordinator(
        DataGridView table,
        IRowExecutionStateProvider rowExecutionStateProvider,
        ICellStateResolver cellStateResolver,
        RecipeViewModel recipeViewModel,
        IReadOnlyList<ColumnDefinition> columns,
        ILogger<TableRenderCoordinator> logger,
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

        AttachEventHandlers();
        _initialized = true;
        ForceInitialFormatting();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_initialized)
            DetachEventHandlers();
    }

    private void AttachEventHandlers()
    {
        _table.CellFormatting += OnCellFormatting;
        _table.CellBeginEdit += OnCellBeginEdit;
        _table.CurrentCellDirtyStateChanged += OnCurrentCellDirtyStateChanged;
        _table.CellPainting += OnCellPaintingPreFormat;
        _rowExecutionStateProvider.CurrentLineChanged += OnCurrentLineChanged;
        _colorSchemeProvider.Changed += OnColorSchemeChanged;
    }

    private void DetachEventHandlers()
    {
        try { _table.CellFormatting -= OnCellFormatting; } catch { /* ignored */ }
        try { _table.CellBeginEdit -= OnCellBeginEdit; } catch { /* ignored */ }
        try { _table.CurrentCellDirtyStateChanged -= OnCurrentCellDirtyStateChanged; } catch { /* ignored */ }
        try { _table.CellPainting -= OnCellPaintingPreFormat; } catch { /* ignored */ }
        try { _rowExecutionStateProvider.CurrentLineChanged -= OnCurrentLineChanged; } catch { /* ignored */ }
        try { _colorSchemeProvider.Changed -= OnColorSchemeChanged; } catch { /* ignored */ }
    }

    private void ForceInitialFormatting()
    {
        if (!_table.IsHandleCreated || _table.IsDisposed)
            return;

        InvokeOnUiThread(FormatAllCells);
    }

    private void FormatAllCells()
    {
        for (int row = 0; row < _table.RowCount; row++)
        {
            for (int col = 0; col < _table.ColumnCount; col++)
            {
                ApplyCellFormattingSafe(row, col);
            }
        }
    }

    private void OnCellPaintingPreFormat(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        ApplyCellFormattingSafe(e.RowIndex, e.ColumnIndex);
    }

    private void OnColorSchemeChanged(ColorScheme colorScheme)
    {
        InvokeOnUiThread(RefreshTableAppearance);
    }

    private void RefreshTableAppearance()
    {
        try
        {
            FormatAllCells();
            _table.Invalidate();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh table appearance after color scheme change");
        }
    }

    private void OnCellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        if (ShouldCancelEdit(e.RowIndex, e.ColumnIndex))
        {
            e.Cancel = true;
            _logger.LogDebug("Edit cancelled for cell [{Row},{Column}]", e.RowIndex, e.ColumnIndex);
        }
    }

    private bool ShouldCancelEdit(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || columnIndex < 0)
            return false;

        try
        {
            if (IsCellReadOnlyByVisualState(rowIndex, columnIndex))
                return true;

            if (IsCellDisabled(rowIndex, columnIndex))
                return true;

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate edit permissions for cell [{Row},{Column}]", rowIndex, columnIndex);
            return true;
        }
    }

    private bool IsCellReadOnlyByVisualState(int rowIndex, int columnIndex)
    {
        var cell = _table.Rows[rowIndex].Cells[columnIndex];
        return cell.Tag is CellVisualState visual && visual.IsReadOnly;
    }

    private bool IsCellDisabled(int rowIndex, int columnIndex)
    {
        var state = _recipeViewModel.GetCellState(rowIndex, columnIndex);
        return state == PropertyState.Disabled;
    }

    private void OnCurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        var cell = _table.CurrentCell;
        if (cell == null || !_table.IsCurrentCellDirty)
            return;

        if (IsCellDisabled(cell.RowIndex, cell.ColumnIndex))
        {
            _table.CancelEdit();
            return;
        }

        if (cell is DataGridViewComboBoxCell or RecipeComboBoxCell or DataGridViewCheckBoxCell)
        {
            try
            {
                _table.CommitEdit(DataGridViewDataErrorContexts.Commit);
                _table.EndEdit();
            }
            catch
            {
                // ignored
            }
        }
    }

    private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        ApplyCellFormattingSafe(e.RowIndex, e.ColumnIndex, e.CellStyle);
    }

    private void ApplyCellFormattingSafe(int rowIndex, int columnIndex, DataGridViewCellStyle? targetStyle = null)
    {
        if (_table.InvokeRequired)
            InvokeOnUiThread(() => ApplyCellFormatting(rowIndex, columnIndex, targetStyle));
        else
            ApplyCellFormatting(rowIndex, columnIndex, targetStyle);
    }

    private void ApplyCellFormatting(int rowIndex, int columnIndex, DataGridViewCellStyle? targetStyle = null)
    {
        if (!IsValidCellCoordinate(rowIndex, columnIndex))
            return;

        if (_table.IsDisposed || !_table.IsHandleCreated)
            return;

        try
        {
            var visual = ResolveCellVisualState(rowIndex, columnIndex);
            ApplyVisualStateToCell(rowIndex, columnIndex, visual, targetStyle);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply formatting for cell [{Row},{Column}]", rowIndex, columnIndex);
        }
    }

    private bool IsValidCellCoordinate(int rowIndex, int columnIndex)
    {
        return columnIndex < _columns.Count && rowIndex < _recipeViewModel.ViewModels.Count;
    }

    private CellVisualState ResolveCellVisualState(int rowIndex, int columnIndex)
    {
        var baseVisual = _cellStateResolver.Resolve(rowIndex, columnIndex, _recipeViewModel);
        var scheme = _colorSchemeProvider.Current;
        var executionState = _rowExecutionStateProvider.GetState(rowIndex);

        // Apply loop nesting overlay only for Upcoming rows (not Current/Passed)
        if (executionState == RowExecutionState.Upcoming)
        {
            var depth = _recipeViewModel.GetLoopNesting(rowIndex);
            if (depth > 0 && depth <= 3)
            {
                // Only overlay if cell is in normal editable state (keep blocked/readonly as-is)
                bool isNormalEditable = !baseVisual.IsReadOnly && baseVisual.BackColor == scheme.LineBgColor;
                if (isNormalEditable)
                {
                    var loopColor = depth switch
                    {
                        1 => scheme.LoopLevel1BgColor,
                        2 => scheme.LoopLevel2BgColor,
                        3 => scheme.LoopLevel3BgColor,
                        _ => scheme.LineBgColor
                    };
                    baseVisual = baseVisual with { BackColor = loopColor };
                }
            }
        }

        // User row selection overlay (after loop coloring), excluded for Current/Passed
        if (IsRowSelectedByUser(rowIndex) && executionState is not (RowExecutionState.Current or RowExecutionState.Passed))
        {
            baseVisual = new CellVisualState(
                Font: baseVisual.Font,
                ForeColor: scheme.RowSelectionTextColor,
                BackColor: scheme.RowSelectionBgColor,
                IsReadOnly: baseVisual.IsReadOnly,
                ComboDisplayStyle: baseVisual.ComboDisplayStyle);
        }

        return baseVisual;
    }

    private bool IsRowSelectedByUser(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _table.Rows.Count)
            return false;

        return _table.Rows[rowIndex].Selected;
    }

    private void ApplyVisualStateToCell(
        int rowIndex,
        int columnIndex,
        CellVisualState visual,
        DataGridViewCellStyle? targetStyle)
    {
        if (targetStyle != null)
            ApplyVisualStateToStyle(visual, targetStyle);

        var cell = _table.Rows[rowIndex].Cells[columnIndex];
        cell.Tag = visual;

        UpdateCellReadOnlyState(rowIndex, columnIndex, cell, visual.IsReadOnly);
        UpdateComboBoxDisplayStyle(cell, visual.ComboDisplayStyle);

        if (targetStyle == null)
            ApplyVisualStateToCellStyle(cell, visual);
    }

    private void ApplyVisualStateToStyle(CellVisualState visual, DataGridViewCellStyle style)
    {
        style.Font = visual.Font;
        style.ForeColor = visual.ForeColor;
        style.BackColor = visual.BackColor;
        style.SelectionBackColor = visual.BackColor;
        style.SelectionForeColor = visual.ForeColor;
    }

    private void UpdateCellReadOnlyState(int rowIndex, int columnIndex, DataGridViewCell cell, bool isReadOnly)
    {
        if (IsCellCurrentlyEditing(rowIndex, columnIndex))
            return;

        cell.ReadOnly = isReadOnly;
    }

    private bool IsCellCurrentlyEditing(int rowIndex, int columnIndex)
    {
        return _table.IsCurrentCellInEditMode
               && _table.CurrentCell.RowIndex == rowIndex
               && _table.CurrentCell.ColumnIndex == columnIndex;
    }

    private void UpdateComboBoxDisplayStyle(DataGridViewCell cell, DataGridViewComboBoxDisplayStyle displayStyle)
    {
        switch (cell)
        {
            case RecipeComboBoxCell recipeCombo:
                recipeCombo.DisplayStyle = displayStyle;
                break;
            case DataGridViewComboBoxCell combo:
                combo.DisplayStyle = displayStyle;
                break;
        }
    }

    private void ApplyVisualStateToCellStyle(DataGridViewCell cell, CellVisualState visual)
    {
        if (!cell.HasStyle)
            return;

        var currentStyle = cell.InheritedStyle;
        if (!ShouldUpdateCellStyle(currentStyle, visual))
            return;

        cell.Style.Font = visual.Font;
        cell.Style.ForeColor = visual.ForeColor;
        cell.Style.BackColor = visual.BackColor;
        cell.Style.SelectionBackColor = visual.BackColor;
        cell.Style.SelectionForeColor = visual.ForeColor;
    }

    private bool ShouldUpdateCellStyle(DataGridViewCellStyle currentStyle, CellVisualState visual)
    {
        return !Equals(currentStyle.Font, visual.Font) ||
               currentStyle.ForeColor != visual.ForeColor ||
               currentStyle.BackColor != visual.BackColor;
    }

    private void OnCurrentLineChanged(int oldIndex, int newIndex)
    {
        InvokeOnUiThread(() => RefreshExecutionStateRows(oldIndex, newIndex));
    }

    private void RefreshExecutionStateRows(int oldIndex, int newIndex)
    {
        try
        {
            RefreshRowIfValid(oldIndex);
            RefreshRowIfValid(newIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh execution state rows (old: {OldIndex}, new: {NewIndex})", oldIndex,
                newIndex);
        }
    }

    private void RefreshRowIfValid(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _table.Rows.Count)
            return;

        FormatRowCells(rowIndex);
        _table.InvalidateRow(rowIndex);
    }

    private void FormatRowCells(int rowIndex)
    {
        for (int col = 0; col < _table.ColumnCount; col++)
        {
            ApplyCellFormattingSafe(rowIndex, col);
        }
    }

    private void InvokeOnUiThread(Action action)
    {
        if (_table.IsDisposed || !_table.IsHandleCreated)
            return;

        if (_table.InvokeRequired)
        {
            try
            {
                _table.BeginInvoke(action);
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogDebug(ex, "Cannot invoke on UI thread - control already disposed");
            }
        }
        else
        {
            action();
        }
    }
}