using System;

using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Table
{
    public class TableColumnManager
    {
        private readonly DataGridView _table;
        private readonly TableSchema _tableSchema;
        private readonly ColorScheme _colorScheme;
        private readonly ComboBoxDataProvider _dataProvider;

        public TableColumnManager(DataGridView table, TableSchema tableSchema, ColorScheme colorScheme, ComboBoxDataProvider dataProvider)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _tableSchema = tableSchema ?? throw new ArgumentNullException(nameof(tableSchema));
            _colorScheme = colorScheme ?? throw new ArgumentNullException(nameof(colorScheme));
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        }

        public void InitializeHeaders()
        {
            _table.ColumnHeadersDefaultCellStyle.Font = _colorScheme.HeaderFont;
            _table.ColumnHeadersDefaultCellStyle.BackColor = _colorScheme.HeaderBgColor;
            _table.ColumnHeadersDefaultCellStyle.ForeColor = _colorScheme.HeaderTextColor;
            _table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _table.RowHeadersVisible = true;
            _table.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;

            _table.EnableHeadersVisualStyles = false; // Prevent windows from reapplying default styles
        }

        public void InitializeTableColumns()
        {
            _table.AutoGenerateColumns = false;
            _table.Columns.Clear();

            foreach (var colDef in _tableSchema.GetReadonlyColumns())
            {
                DataGridViewColumn column;

                // Comboboxes
                if (colDef.TableCellType == typeof(DataGridViewComboBoxCell))
                {
                    column = new DataGridViewComboBoxColumn();
                }
                else
                {
                    column = new DataGridViewTextBoxColumn();
                }

                // Action
                if (colDef.Key == ColumnKey.Action)
                {
                    var cboxColumn = new DataGridViewComboBoxColumn();
                    cboxColumn.DataSource = new BindingSource(_dataProvider.Actions, null);
                    cboxColumn.ValueMember = "Key";
                    cboxColumn.DisplayMember = "Value";
                    column = cboxColumn;
                }
                // ActionTarget
                else if (colDef.Key == ColumnKey.ActionTarget)
                {
                    // Datasource for ActionTarget is dynamic and depends on the selected action
                    var cboxColumn = new DataGridViewComboBoxColumn();
                    cboxColumn.ValueMember = "Key";
                    cboxColumn.DisplayMember = "Value";
                    column = cboxColumn;
                }

                column.Name = colDef.Key.ToString();
                column.HeaderText = colDef.UiName;

                column.DataPropertyName = colDef.Key.ToString();

                column.ReadOnly = colDef.ReadOnly;
                column.SortMode = DataGridViewColumnSortMode.NotSortable;

                if (column.Width > 0)
                {
                    column.Width = column.Width;
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                }
                else if (column.Width == -1)
                {
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }

                column.DefaultCellStyle.Alignment = colDef.Alignment;
                column.DefaultCellStyle.Font = _colorScheme.LineFont;
                column.DefaultCellStyle.BackColor = _colorScheme.LineBgColor;
                column.DefaultCellStyle.ForeColor = _colorScheme.LineTextColor;

                _table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                _table.AllowUserToAddRows = false;
                _table.AllowUserToDeleteRows = false;
                _table.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                _table.MultiSelect = false;

                _table.Columns.Add(column);
            }
        }
    }
}