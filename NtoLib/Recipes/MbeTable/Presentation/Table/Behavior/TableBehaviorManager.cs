#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;
using NtoLib.Recipes.MbeTable.Presentation.Status;
using NtoLib.Recipes.MbeTable.Presentation.Table.CellState;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Behavior
{
    /// <summary>
    /// The central point of ALL table behavior.
    ///
    /// Goals:
    /// 1. Unified application of colors/fonts (DO NOT mutate cell.Style, only e.CellStyle or custom drawing).
    /// 2. "Row-level override" for states:
    ///      - Current (current PLC line)
    ///      - Passed (past lines)
    ///    In these rows, ALL cells must be colored uniformly, regardless of the Disabled / ReadOnly statuses of the properties themselves.
    ///    This is done intentionally, otherwise DataGridView partially caches formatting and some cells (especially text cells) remain "white".
    /// 3. Custom rendering of ComboBoxes in ReadOnly (remove arrow, flat fill).
    /// 4. Focus/cursor — draw a neat border on top (SelectedOutline).
    /// 5. Minimum redrawing: when the current line changes — invalidate only the range.
    /// 6. Avoid "sticky" styles — do not touch cell.Style (styles are not permanently cached in the DGV internal dictionary).
    ///
    /// Why we had to use OnCellPainting:
    ///  - Standard DataGridView DOES NOT guarantee a repeated call to CellFormatting when the "global state" of a row changes,
    ///    if the cell value does not change.
    ///  - Some internal optimizations overwrite BackColor set in e.CellStyle when FullRowSelect is active,
    ///    or during editing (TextBox gets a system white background).
    ///  - Therefore, for Current/Passed rows, we take over the entire rendering (background + text + border).
    ///  - For others — we let the standard mechanism work (e.Paint) and only supplement with a border.
    ///
    /// Painting stages:
    ///   OnCellFormatting:
    ///       - Get logical state (state manager) or force row-level colors.
    ///       - Set e.CellStyle.*
    ///       - Set ReadOnly / DisplayStyle (combo).
    ///   OnCellPainting:
    ///       - If the row is Current/Passed (or a combobox in readonly) — manually fill the background (guaranteed).
    ///       - Otherwise: standard drawing + focus outline.
    ///
    /// Important: Any edits that "simplify" this code will almost certainly bring back the bug
    /// of partially uncolored cells during execution. Do not change without regression tests!
    /// The OnViewModelUpdateEnd method was added as a final, targeted fix for this exact problem.
    /// </summary>
    public sealed class TableBehaviorManager : IDisposable
    {
        private readonly DataGridView _table;
        private readonly TableColumns _columns;
        private readonly TableCellStateManager _stateManager;
        private readonly IPlcRecipeStatusProvider _statusProvider;
        private readonly IStatusManager? _statusManager;
        private readonly ILogger? _debugLogger;
        private readonly ICellStylePalette _palette;
        private readonly RecipeViewModel _recipeViewModel;
        private ColorScheme _colorScheme;

        private bool _attached;
        private bool _disposed;
        private bool _clearedInitialPerCellStyles;
        private PlcRecipeStatus? _lastStatus;
        
        private readonly Func<int>? _getComboMaxItems;

        public TableBehaviorManager(
            DataGridView table,
            TableColumns columns,
            TableCellStateManager stateManager,
            IStatusManager? statusManager,
            ICellStylePalette palette,
            ColorScheme colorScheme,
            IPlcRecipeStatusProvider statusProvider,
            RecipeViewModel recipeViewModel,
            ILogger? debugLogger = null,
            Func<int>? getComboMaxItems = null)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _columns = columns ?? throw new ArgumentNullException(nameof(columns));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _statusManager = statusManager;
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            _colorScheme = colorScheme ?? throw new ArgumentNullException(nameof(colorScheme));
            _statusProvider = statusProvider ?? throw new ArgumentNullException(nameof(statusProvider));
            _recipeViewModel = recipeViewModel ?? throw new ArgumentNullException(nameof(recipeViewModel));
            _debugLogger = debugLogger;
            _getComboMaxItems = getComboMaxItems;

            _table.Disposed += OnTableDisposed;
        }

        public void TableStyleSetup()
        {
            _table.EditMode = DataGridViewEditMode.EditOnEnter;
            _table.EnableHeadersVisualStyles = false;

            void Equalize(DataGridViewCellStyle s)
            {
                s.SelectionBackColor = s.BackColor;
                s.SelectionForeColor = s.ForeColor;
            }
            Equalize(_table.DefaultCellStyle);
            Equalize(_table.RowsDefaultCellStyle);
            Equalize(_table.ColumnHeadersDefaultCellStyle);
            Equalize(_table.RowHeadersDefaultCellStyle);
        }

        public void Attach()
        {
            if (_disposed || _attached) return;

            _table.CellFormatting += OnCellFormatting;
            _table.CellBeginEdit += OnCellBeginEdit;
            _table.CellPainting += OnCellPainting;
            _table.DataError += OnDataError;
            _table.RowPostPaint += OnRowPostPaint;
            _table.SelectionChanged += OnSelectionChanged;
            _table.DataBindingComplete += OnDataBindingComplete;
            _table.EditingControlShowing += OnEditingControlShowing;

            _statusProvider.StatusChanged += OnStatusChanged;
            _recipeViewModel.OnUpdateEnd += OnViewModelUpdateEnd;
            _lastStatus = _statusProvider.GetStatus();

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
            _table.SelectionChanged -= OnSelectionChanged;
            _table.DataBindingComplete -= OnDataBindingComplete;
            _table.EditingControlShowing -= OnEditingControlShowing;

            _statusProvider.StatusChanged -= OnStatusChanged;
            _recipeViewModel.OnUpdateEnd -= OnViewModelUpdateEnd;
            _attached = false;
        }

        /// <summary>
        /// This method is the definitive fix for the "sticky cell state" bug.
        /// PROBLEM: When an Action is changed, the underlying StepViewModel is replaced.
        /// However, DataGridView aggressively caches cell states. If a cell's display value
        /// doesn't change (e.g., empty to empty), DGV will often skip calling OnCellFormatting,
        /// leaving the old ReadOnly status "stuck" on the cell. Standard invalidation methods
        /// are not sufficient to reliably break this cache.
        /// SOLUTION: After the ViewModel has finished its update, we check if the last operation
        /// was a 'Replace'. If so, we bypass DGV's event system entirely. We manually iterate
        /// through each cell of the affected row, get the true state from the new StepViewModel,
        /// and directly set the cell's ReadOnly property. This guarantees state synchronization.
        /// </summary>
        private void OnViewModelUpdateEnd()
        {
            var lastChange = _recipeViewModel.LastChange;
            if (lastChange?.Type != RecipeViewModel.ChangeType.Replace)
            {
                return;
            }

            int rowIndex = lastChange.Index;
            if (rowIndex < 0 || rowIndex >= _table.Rows.Count)
            {
                return;
            }

            var row = _table.Rows[rowIndex];
            if (row.DataBoundItem is not StepViewModel vm)
            {
                return;
            }

            _debugLogger?.Log($"OnViewModelUpdateEnd: Manually synchronizing ReadOnly state for row {rowIndex} after Action change.");

            // Manually iterate through each cell and set its ReadOnly property
            // from the source of truth (the ViewModel).
            for (int i = 0; i < _table.Columns.Count; i++)
            {
                var cell = row.Cells[i];
                var columnKey = _columns.GetColumnDefinition(i).Key;
                var visualState = _stateManager.GetStateForCell(vm, columnKey, rowIndex);

                if (cell.ReadOnly != visualState.IsReadonly)
                {
                    cell.ReadOnly = visualState.IsReadonly;
                }
                
                // Also synchronize visual styles for specific cell types that depend on ReadOnly state.
                if (cell is DataGridViewComboBoxCell comboCell)
                {
                    var desiredStyle = visualState.IsReadonly
                        ? DataGridViewComboBoxDisplayStyle.Nothing
                        : DataGridViewComboBoxDisplayStyle.DropDownButton;
                    
                    if (comboCell.DisplayStyle != desiredStyle)
                    {
                        comboCell.DisplayStyle = desiredStyle;
                    }
                }
            }
            
            // Finally, force a repaint of the now-correctly-configured row.
            _table.InvalidateRow(rowIndex);
        }
        
        private void OnTableDisposed(object? sender, EventArgs e)
        {
            try { Detach(); } catch { }
        }

        public void RefreshTheme(ColorScheme? schemeOverride = null)
        {
            if (schemeOverride != null)
                _colorScheme = schemeOverride;
            TableStyleSetup();
            try { _table.Invalidate(true); } catch { }
        }

        private void OnDataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (_clearedInitialPerCellStyles) return;
            _clearedInitialPerCellStyles = true;
            try
            {
                foreach (DataGridViewRow row in _table.Rows)
                    foreach (DataGridViewCell cell in row.Cells)
                        if (cell.HasStyle)
                            cell.Style = new DataGridViewCellStyle();
            }
            catch { }
        }

        private void OnSelectionChanged(object? sender, EventArgs e)
        {
            try
            {
                var cur = _table.CurrentCell;
                if (cur != null) _table.InvalidateCell(cur);
            }
            catch { }
        }

        private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var grid = (DataGridView)sender!;
            var row = grid.Rows[e.RowIndex];
            if (row.DataBoundItem is not StepViewModel vm) return;

            var status = _statusProvider.GetStatus();
            bool rowPassed = e.RowIndex < status.CurrentLine;
            bool rowCurrent = e.RowIndex == status.CurrentLine;

            CellStatusDescription visual;

            if (rowPassed || rowCurrent)
            {
                var forcedState = rowCurrent ? TableCellState.Current : TableCellState.Passed;
                visual = _palette.Resolve(forcedState) with { IsReadonly = true };
            }
            else
            {
                var columnKey = _columns.GetColumnDefinition(e.ColumnIndex).Key;
                visual = _stateManager.GetStateForCell(vm, columnKey, e.RowIndex);
            }

            e.CellStyle.Font = visual.Font;
            e.CellStyle.ForeColor = visual.ForeColor;
            e.CellStyle.BackColor = visual.BackColor;
            e.CellStyle.SelectionBackColor = visual.BackColor;
            e.CellStyle.SelectionForeColor = visual.ForeColor;

            var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (cell.ReadOnly != visual.IsReadonly)
                cell.ReadOnly = visual.IsReadonly;

            if (cell is DataGridViewComboBoxCell comboCell)
            {
                var desired = visual.IsReadonly
                    ? DataGridViewComboBoxDisplayStyle.Nothing
                    : DataGridViewComboBoxDisplayStyle.DropDownButton;
                if (comboCell.DisplayStyle != desired)
                    comboCell.DisplayStyle = desired;
            }
        }

        private void OnCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var grid = (DataGridView)sender!;
            if (grid.Rows[e.RowIndex].DataBoundItem is not StepViewModel vm)
                return;

            var status = _statusProvider.GetStatus();
            bool rowPassed = e.RowIndex < status.CurrentLine;
            bool rowCurrent = e.RowIndex == status.CurrentLine;

            var columnKey = _columns.GetColumnDefinition(e.ColumnIndex).Key;
            var cellVisual = _stateManager.GetStateForCell(vm, columnKey, e.RowIndex);

            bool forceRowStyle = rowPassed || rowCurrent;
            CellStatusDescription rowVisual = cellVisual;

            if (forceRowStyle)
            {
                var forcedState = rowCurrent ? TableCellState.Current : TableCellState.Passed;
                rowVisual = _palette.Resolve(forcedState) with { IsReadonly = true };
            }

            bool isCombo =
                grid.Columns[e.ColumnIndex] is DataGridViewComboBoxColumn ||
                grid.Rows[e.RowIndex].Cells[e.ColumnIndex] is DataGridViewComboBoxCell;

            bool customComboReadonly = isCombo && rowVisual.IsReadonly;

            if (forceRowStyle || customComboReadonly)
            {
                e.Handled = true;

                using (var back = new SolidBrush(rowVisual.BackColor))
                    e.Graphics.FillRectangle(back, e.CellBounds);

                e.Paint(e.CellBounds, DataGridViewPaintParts.Border);

                var text = e.FormattedValue?.ToString() ?? string.Empty;
                var rect = Rectangle.Inflate(e.CellBounds, -4, -2);

                TextRenderer.DrawText(
                    e.Graphics,
                    text,
                    rowVisual.Font ?? grid.Font,
                    rect,
                    rowVisual.ForeColor,
                    rowVisual.BackColor,
                    TextFormatFlags.Left |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPrefix);

                DrawFocusOutlineIfCurrent(grid, e);
                return;
            }

            e.Paint(e.ClipBounds, e.PaintParts);
            DrawFocusOutlineIfCurrent(grid, e);
            e.Handled = true;
        }

        private void DrawFocusOutlineIfCurrent(DataGridView grid, DataGridViewCellPaintingEventArgs e)
        {
            var cur = grid.CurrentCell;
            if (cur == null || cur.RowIndex != e.RowIndex || cur.ColumnIndex != e.ColumnIndex) return;

            using var pen = new Pen(_colorScheme.SelectedOutlineColor, Math.Max(1, _colorScheme.SelectedOutlineThickness));
            var rect = Rectangle.Inflate(e.CellBounds, -1, -1);
            e.Graphics.DrawRectangle(pen, rect);
        }

        private void OnEditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (_table.CurrentCell is null) return;

            if (e.Control is DataGridViewTextBoxEditingControl tb)
            {
                var style = _table.CurrentCell.InheritedStyle;
                try
                {
                    tb.BackColor = style.BackColor;
                    tb.ForeColor = style.ForeColor;
                    tb.Font = style.Font;
                }
                catch { }
            }

            if (e.Control is ComboBox cb)
            {
                try
                {
                    int desired = _getComboMaxItems?.Invoke() ?? cb.MaxDropDownItems;
                    if (desired <= 0) desired = 1;

                    int actual = cb.Items.Count;
                    int visible = Math.Min(desired, actual);

                    cb.MaxDropDownItems = visible;
                    cb.IntegralHeight = false;

                    if (cb.ItemHeight > 0)
                    {
                        int extra = 2;
                        cb.DropDownHeight = cb.ItemHeight * visible + extra;
                    }
                }
                catch (Exception ex)
                {
                    _debugLogger?.Log($"ComboBox MaxDropDownItems adjust failed: {ex.Message}");
                }
            }
        }

        private void OnDataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var grid = (DataGridView)sender!;
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
                e.Cancel = true;
            }
        }

        private void OnCellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_table.Rows[e.RowIndex].DataBoundItem is not StepViewModel vm) return;

            var key = _columns.GetColumnDefinition(e.ColumnIndex).Key;
            var visual = _stateManager.GetStateForCell(vm, key, e.RowIndex);
            if (visual.IsReadonly)
                e.Cancel = true;
        }

        private void OnRowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = (DataGridView)sender!;
            var text = (e.RowIndex + 1).ToString();
            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);

            var style = grid.RowHeadersDefaultCellStyle;
            var font = style?.Font ?? grid.Font;
            var fore = style?.ForeColor.IsEmpty == false ? style.ForeColor : grid.ForeColor;

            TextRenderer.DrawText(
                e.Graphics,
                text,
                font,
                headerBounds,
                fore,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
        }

        private void OnStatusChanged(PlcRecipeStatus status)
        {
            try
            {
                var prev = _lastStatus;
                _lastStatus = status;

                if (prev == null || _table.Rows.Count == 0)
                {
                    _table.Invalidate();
                    return;
                }

                if (prev.IsRecipeActive != status.IsRecipeActive)
                {
                    _table.Invalidate();
                    return;
                }

                var oldLine = prev.CurrentLine;
                var newLine = status.CurrentLine;
                if (oldLine == newLine) return;

                int rowCount = _table.Rows.Count;
                int from = Math.Min(oldLine, newLine);
                int to = Math.Max(oldLine, newLine);
                from = Math.Max(0, from);
                to = Math.Min(rowCount - 1, to);

                for (int i = from; i <= to; i++)
                {
                    try { _table.InvalidateRow(i); } catch { }
                }
            }
            catch (Exception ex)
            {
                _debugLogger?.LogException(ex, "OnStatusChanged invalidate failed");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { Detach(); } catch { }
            try { _table.Disposed -= OnTableDisposed; } catch { }
        }
    }
}