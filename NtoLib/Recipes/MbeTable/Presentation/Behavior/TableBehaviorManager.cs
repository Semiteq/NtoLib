﻿using System;
using System.Drawing;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.Journaling.Status;
using NtoLib.Recipes.MbeTable.Presentation.Cells;
using NtoLib.Recipes.MbeTable.Presentation.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Behavior;

/// <summary>
/// Handles minimal DataGridView behaviors: numbering, focus outline (except disabled combo custom paints),
/// data errors, editing control styling.
/// </summary>
public sealed class TableBehaviorManager : IDisposable
{
    private readonly DataGridView _table;
    private readonly IStatusManager? _statusManager;
    private readonly ColorScheme _colorScheme;

    private bool _attached;
    private bool _disposed;

    public TableBehaviorManager(
        DataGridView table,
        IStatusManager? statusManager,
        ColorScheme colorScheme)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
        _statusManager = statusManager;
        _colorScheme = colorScheme ?? throw new ArgumentNullException(nameof(colorScheme));

        _table.Disposed += OnTableDisposed;
    }

    ~TableBehaviorManager()
    {
        Dispose(false);
    }

    public void Attach()
    {
        if (_disposed || _attached) return;

        _table.CellPainting += OnCellPainting;
        _table.DataError += OnDataError;
        _table.RowPostPaint += OnRowPostPaint;
        _table.EditingControlShowing += OnEditingControlShowing;

        _attached = true;
    }

    public void Detach()
    {
        if (!_attached) return;

        _table.CellPainting -= OnCellPainting;
        _table.DataError -= OnDataError;
        _table.RowPostPaint -= OnRowPostPaint;
        _table.EditingControlShowing -= OnEditingControlShowing;

        _attached = false;
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
        
        if (disposing)
        {
            try { Detach(); } catch { /* ignored */ }
            try { _table.Disposed -= OnTableDisposed; } catch { /* ignored */ }
        }
    }

    private void OnTableDisposed(object? sender, EventArgs e)
    {
        try { Detach(); } catch { /* ignored */ }
    }

    private void OnCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var cell = _table.Rows[e.RowIndex].Cells[e.ColumnIndex];

        if (cell is RecipeComboBoxCell)
            return;

        e.Paint(e.ClipBounds, e.PaintParts);
        DrawFocusOutlineIfCurrent(_table, e);
        e.Handled = true;
    }

    private void DrawFocusOutlineIfCurrent(DataGridView grid, DataGridViewCellPaintingEventArgs e)
    {
        var currentCell = grid.CurrentCell;
        if (currentCell == null || currentCell.RowIndex != e.RowIndex || currentCell.ColumnIndex != e.ColumnIndex)
            return;

        using var pen = new Pen(_colorScheme.SelectedOutlineColor, Math.Max(1, _colorScheme.SelectedOutlineThickness));
        var rect = Rectangle.Inflate(e.CellBounds, -1, -1);
        e.Graphics.DrawRectangle(pen, rect);
    }

    private void OnEditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (_table.CurrentCell is null) return;

        if (e.Control is DataGridViewTextBoxEditingControl textBox)
        {
            var style = _table.CurrentCell.InheritedStyle;
            try
            {
                textBox.BackColor = style.BackColor;
                textBox.ForeColor = style.ForeColor;
                textBox.Font = style.Font;
            }
            catch { /* ignored */ }
        }

        if (e.Control is ComboBox comboBox)
        {
            var desired = comboBox.MaxDropDownItems;
            if (desired <= 0) desired = 1;

            var actual = comboBox.Items.Count;
            var visible = Math.Max(1, Math.Min(desired, actual));

            comboBox.MaxDropDownItems = visible;
            comboBox.IntegralHeight = false;

            if (comboBox.ItemHeight > 0)
            {
                const int extra = 2;
                comboBox.DropDownHeight = comboBox.ItemHeight * visible + extra;
            }
        }
    }

    private void OnDataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

        var grid = (DataGridView)sender!;
        
        if (grid.Columns.Count <= e.ColumnIndex || grid.Rows.Count <= e.RowIndex)
        {
            e.ThrowException = false;
            e.Cancel = true;
            return;
        }

        var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];

        if (cell is DataGridViewComboBoxCell || grid.Columns[e.ColumnIndex] is DataGridViewComboBoxColumn)
        {
            e.ThrowException = false;
            e.Cancel = true;
            return;
        }

        if (e.Exception is FormatException)
        {
            _statusManager?.WriteStatusMessage(e.Exception.Message, StatusMessage.Error);

            e.ThrowException = false;
            
            try
            {
                grid.CancelEdit();
                grid.EndEdit();
                grid.InvalidateCell(e.ColumnIndex, e.RowIndex);
            }
            catch { /* ignored */ }
        }
        else
        {
            e.ThrowException = false;
            e.Cancel = true;
        }
    }

    private void OnRowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
    {
        var grid = (DataGridView)sender!;
        var text = (e.RowIndex + 1).ToString();
        var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);

        var style = grid.RowHeadersDefaultCellStyle;
        var font = style?.Font ?? grid.Font;
        var foreColor = style?.ForeColor.IsEmpty == false ? style.ForeColor : grid.ForeColor;

        TextRenderer.DrawText(
            e.Graphics,
            text,
            font,
            headerBounds,
            foreColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
    }
}