using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Table
{
    public class TableColumnManager
    {
        private readonly DataGridView _table;
        private readonly TableSchema _tableSchema;

        private readonly ColorScheme _colorScheme;

        public TableColumnManager(DataGridView table, TableSchema tableSchema, ColorScheme colorScheme)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _tableSchema = tableSchema ?? throw new ArgumentNullException(nameof(tableSchema));
            _colorScheme = colorScheme ?? throw new ArgumentNullException(nameof(colorScheme));
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
                    var cboxColumn = new DataGridViewComboBoxColumn();
                    //todo: value mapping for ComboBox
                    column = cboxColumn;
                }
                else
                {
                    column = new DataGridViewTextBoxColumn();
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
    
        // public void SetActionColumnData(Dictionary<int, string> actionMap)
        // {
        //     if (actionMap == null || _table.Columns.Count == 0) return;
        //
        //     var actionColumn = _table.Columns[0] as DataGridViewComboBoxColumn;
        //     if (actionColumn == null) return;
        //
        //     actionColumn.Items.Clear();
        //     foreach (var item in actionMap.Values)
        //     {
        //         actionColumn.Items.Add(item);
        //     }
        //
        //     // Обновляем TableColumn с новыми данными
        //     if (_columns.Count > 0)
        //     {
        //         var updatedColumn = new TableColumn(_columns[0].Name, actionMap)
        //         {
        //             GridIndex = _columns[0].GridIndex
        //         };
        //         _columns[0] = updatedColumn;
        //         actionColumn.Tag = updatedColumn;
        //     }
        // }
    
        public void SetTargetColumnData(Dictionary<int, string> targetMap)
        {
            if (targetMap == null || _table.Columns.Count <= 1) return;
    
            var targetColumn = _table.Columns[1] as DataGridViewComboBoxColumn;
            if (targetColumn == null) return;
    
            targetColumn.Items.Clear();
            foreach (var item in targetMap.Values)
            {
                targetColumn.Items.Add(item);
            }
        }
    
    }
}