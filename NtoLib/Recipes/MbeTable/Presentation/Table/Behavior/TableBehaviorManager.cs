using System;
using System.Drawing;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Presentation.Table.State;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Behavior
{
    /// <summary>
    /// Applies visual state and editing rules for table cells and suppresses known data binding glitches.
    /// </summary>
    public sealed class TableBehaviorManager : IDisposable
    {
        private readonly DataGridView _table;
        private readonly TableSchema _schema;
        private readonly TableCellStateManager _stateManager;
        private readonly DebugLogger? _debugLogger;

        private bool _attached;
        private bool _disposed;

        public TableBehaviorManager(
            DataGridView table,
            TableSchema schema,
            TableCellStateManager stateManager,
            DebugLogger? debugLogger = null)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _debugLogger = debugLogger;

            // Auto-detach when the grid is disposed.
            _table.Disposed += OnTableDisposed;
        }

        /// <summary>
        /// One-time table style configuration.
        /// </summary>
        public void TableStyleSetup()
        {
            _table.EditMode = DataGridViewEditMode.EditOnEnter;
            _table.DefaultCellStyle.SelectionBackColor = _table.DefaultCellStyle.BackColor;
            _table.DefaultCellStyle.SelectionForeColor = _table.DefaultCellStyle.ForeColor;
        }

        public void Attach()
        {
            if (_disposed || _attached) return;

            _table.CellFormatting += OnCellFormatting;
            _table.CellBeginEdit += OnCellBeginEdit;
            _table.CellPainting += OnCellPainting; // Gray out blocked ActionTarget
            _table.DataError += OnDataError;       // Suppress ComboBox binding glitches
            _table.RowPostPaint += OnRowPostPaint;

            _attached = true;
        }

        public void Detach()
        {
            if (!_attached) return;

            _table.CellFormatting -= OnCellFormatting;
            _table.CellBeginEdit -= OnCellBeginEdit;
            _table.CellPainting -= OnCellPainting;
            _table.DataError -= OnDataError;
            _table.RowPostPaint -= OnRowPostPaint;

            _attached = false;
        }

        private void OnTableDisposed(object? sender, EventArgs e)
        {
            // Grid is going away — make sure we unhook.
            try { Detach(); } catch { /* ignore */ }
        }

        private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var grid = (DataGridView)sender!;
            var row = grid.Rows[e.RowIndex];
            if (row.DataBoundItem is not StepViewModel vm) return;

            var columnKey = _schema.GetColumnDefinition(e.ColumnIndex).Key;
            var cellState = _stateManager.GetStateForCell(vm, columnKey);

            // Cheap path: always set on e.CellStyle
            e.CellStyle.Font = cellState.Font;
            e.CellStyle.ForeColor = cellState.ForeColor;
            e.CellStyle.BackColor = cellState.BackColor;
            if (cellState.IsReadonly)
            {
                e.CellStyle.SelectionBackColor = cellState.BackColor;
                e.CellStyle.SelectionForeColor = cellState.ForeColor;
            }

            // Expensive path: only touch cell.Style when values actually differ
            var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            var s = cell.Style;
            bool changed = false;

            if (s.Font != cellState.Font) { s.Font = cellState.Font; changed = true; }
            if (s.ForeColor != cellState.ForeColor) { s.ForeColor = cellState.ForeColor; changed = true; }
            if (s.BackColor != cellState.BackColor) { s.BackColor = cellState.BackColor; changed = true; }

            if (cellState.IsReadonly)
            {
                if (s.SelectionBackColor != cellState.BackColor) { s.SelectionBackColor = cellState.BackColor; changed = true; }
                if (s.SelectionForeColor != cellState.ForeColor) { s.SelectionForeColor = cellState.ForeColor; changed = true; }
            }
            else
            {
                if (s.SelectionBackColor != Color.Empty) { s.SelectionBackColor = Color.Empty; changed = true; }
                if (s.SelectionForeColor != Color.Empty) { s.SelectionForeColor = Color.Empty; changed = true; }
            }

            if (changed)
                cell.Style = s;

            if (cell.ReadOnly != cellState.IsReadonly)
                cell.ReadOnly = cellState.IsReadonly;

            if (cell is DataGridViewComboBoxCell comboCell)
            {
                var isDisabled = vm.IsPropertyDisabled(columnKey);
                var desired = (cellState.IsReadonly || isDisabled)
                    ? DataGridViewComboBoxDisplayStyle.Nothing
                    : DataGridViewComboBoxDisplayStyle.DropDownButton;

                if (comboCell.DisplayStyle != desired)
                {
                    comboCell.DisplayStyle = desired;
                    _debugLogger?.Log($"[OnCellFormatting] updated r{e.RowIndex} c{columnKey} readonly={cellState.IsReadonly}");
                }
            }
        }

        private void OnCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var grid = (DataGridView)sender!;
            var row = grid.Rows[e.RowIndex];
            if (row.DataBoundItem is not StepViewModel vm) return;

            var columnKey = _schema.GetColumnDefinition(e.ColumnIndex).Key;
            if (columnKey != ColumnKey.ActionTarget) return;

            var state = _stateManager.GetStateForCell(vm, columnKey);
            if (!state.IsReadonly) return;

            using (var back = new SolidBrush(state.BackColor))
            {
                e.Graphics.FillRectangle(back, e.CellBounds);
            }

            e.Paint(e.CellBounds, DataGridViewPaintParts.Border);

            var text = e.FormattedValue?.ToString() ?? string.Empty;
            var textRect = Rectangle.Inflate(e.CellBounds, -4, -2);
            var font = state.Font ?? grid.Font;

            TextRenderer.DrawText(
                e.Graphics,
                text,
                font,
                textRect,
                state.ForeColor,
                state.BackColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.Left);

            e.Handled = true;
        }

        private void OnDataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var colKey = _schema.GetColumnDefinition(e.ColumnIndex).Key;
            if (colKey == ColumnKey.ActionTarget)
            {
                // Suppress common "value is not valid" when the list changes under the cell.
                e.ThrowException = false;
                e.Cancel = true;
                _debugLogger?.Log($"[OnDataError] ActionTarget r{e.RowIndex} suppressed: {e.Exception?.Message}");
            }
        }

        private void OnCellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var grid = (DataGridView)sender!;
            var row = grid.Rows[e.RowIndex];
            if (row.DataBoundItem is not StepViewModel vm) return;

            var key = _schema.GetColumnDefinition(e.ColumnIndex).Key;
            var state = _stateManager.GetStateForCell(vm, key);

            if (state.IsReadonly)
                e.Cancel = true;
        }

        private void OnRowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = (DataGridView)sender!;
            string indexText = (e.RowIndex + 1).ToString();

            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);

            var style = grid.RowHeadersDefaultCellStyle;
            var font = style?.Font ?? grid.Font;
            var fore = style?.ForeColor.IsEmpty == false ? style.ForeColor : grid.ForeColor;

            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;

            TextRenderer.DrawText(e.Graphics, indexText, font, headerBounds, fore, flags);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try { Detach(); } catch { /* ignore */ }

            try { _table.Disposed -= OnTableDisposed; } catch { /* ignore */ }
        }
    }
}