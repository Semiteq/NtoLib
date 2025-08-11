using System;
using System.Drawing;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Presentation.Table
{
    /// <summary>
    /// Manages table behavior by attaching or detaching event handlers for a given
    /// DataGridView, according to a specified table schema and cell state manager.
    /// </summary>
    public sealed class TableBehaviorManager
    {
        private readonly DataGridView _table;
        private readonly TableSchema _schema;
        private readonly TableCellStateManager _stateManager;

        public TableBehaviorManager(DataGridView table, TableSchema schema, TableCellStateManager stateManager)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        }

        public void Attach()
        {
            Detach();
            _table.CellFormatting += OnCellFormatting;
            _table.CellBeginEdit += OnCellBeginEdit;
            _table.EditingControlShowing += OnEditingControlShowing;
            _table.RowPostPaint += OnRowPostPaint; // нумерация строк
        }

        public void Detach()
        {
            _table.CellFormatting -= OnCellFormatting;
            _table.CellBeginEdit -= OnCellBeginEdit;
            _table.EditingControlShowing -= OnEditingControlShowing;
            _table.RowPostPaint -= OnRowPostPaint;
        }

        private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var grid = (DataGridView)sender!;
            var row = grid.Rows[e.RowIndex];
            if (row.DataBoundItem is not StepViewModel vm) return;

            var columnKey = _schema.GetColumnDefinition(e.ColumnIndex).Key;
            var cellState = _stateManager.GetStateForCell(vm, columnKey);

            e.CellStyle.Font = cellState.Font;
            e.CellStyle.ForeColor = cellState.ForeColor;
            e.CellStyle.BackColor = cellState.BackColor;

            var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (cell.ReadOnly != cellState.IsReadonly)
                cell.ReadOnly = cellState.IsReadonly;

            if (cellState.IsReadonly)
            {
                e.CellStyle.SelectionBackColor = cellState.BackColor;
                e.CellStyle.SelectionForeColor = cellState.ForeColor;
            }

            if (cell is DataGridViewComboBoxCell comboCell)
            {
                var isDisabled = vm.IsPropertyDisabled(columnKey);
                if (cellState.IsReadonly || isDisabled)
                {
                    if (comboCell.DisplayStyle != DataGridViewComboBoxDisplayStyle.Nothing)
                        comboCell.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                }
                else
                {
                    if (comboCell.DisplayStyle != DataGridViewComboBoxDisplayStyle.DropDownButton)
                        comboCell.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
                }
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

        private void OnEditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is ComboBox cb)
            {
                cb.DrawMode = DrawMode.Normal;
                cb.FlatStyle = FlatStyle.Standard;
                cb.DropDownStyle = ComboBoxStyle.DropDownList;

                var grid = (DataGridView)sender!;
                var style = grid.CurrentCell?.InheritedStyle;
                if (style != null)
                {
                    try
                    {
                        cb.BackColor = style.BackColor;
                        cb.ForeColor = style.ForeColor;
                        cb.Font = style.Font ?? cb.Font;
                    }
                    catch
                    {
                        // ignore styling errors
                    }
                }
            }
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
    }
}