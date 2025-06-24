using System.Collections.Generic;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Recipe.PropertyUnion;
using NtoLib.Recipes.MbeTable.Schema;
using NtoLib.Recipes.MbeTable.Table.UI.StatusManager;

namespace NtoLib.Recipes.MbeTable.Table
{
    public class TableColumnManager
    {
        private readonly DataGridView _dataGridView;
        private readonly IStatusManager _statusManager;
        private List<TableColumn> _columns;
            
        public TableColumnManager(DataGridView dataGridView, IStatusManager statusManager)
        {
            _dataGridView = dataGridView;
            _statusManager = statusManager;
        }

        public void ConfigureColumns(TableMode editMode)
        {
            _statusManager.WriteStatusMessage("Подготовка таблицы. ");
            
            ClearDataGrid();
            InitializeColumns();
            CreateGridColumns(editMode);
            ApplyCellAlignment();
            
            _statusManager.WriteStatusMessage("Таблица подготовлена. ");
        }

        private void ClearDataGrid()
        {
            _dataGridView.Rows.Clear();
            _dataGridView.Columns.Clear();
        }

        private void InitializeColumns()
        {
            _columns = new List<TableColumn>();
            
            foreach (var definition in TableSchema.Configuration.Columns.OrderBy(c => c.Index))
            {
                var column = new TableColumn(definition.UiName, definition.PropertyType);
                _columns.Add(column);
            }
        }

        private void CreateGridColumns(TableMode editMode)
        {
            bool isTableReadOnly = editMode != TableMode.Edit;
            
            foreach (var definition in TableSchema.Configuration.Columns.OrderBy(c => c.Index))
            {
                var column = _columns[definition.Index];
                
                DataGridViewColumn gridColumn = isTableReadOnly && definition.IsEditable
                    ? CreateEditableColumn(column, definition) 
                    : CreateReadOnlyColumn(column, definition);
                    
                column.GridIndex = _dataGridView.Columns.Add(gridColumn);
            }
        }

        private DataGridViewColumn CreateEditableColumn(TableColumn column, ColumnDefinition definition)
        {
            return definition.PropertyType switch
            {
                PropertyType.Bool => CreateBoolComboBoxColumn(column, definition),
                PropertyType.Enum => CreateEnumComboBoxColumn(column, definition),
                _ => CreateTextBoxColumn(column, definition)
            };
        }

        private DataGridViewColumn CreateReadOnlyColumn(TableColumn column, ColumnDefinition definition)
        {
            return CreateTextBoxColumn(column, definition);
        }

        private DataGridViewComboBoxColumn CreateBoolComboBoxColumn(TableColumn column, ColumnDefinition definition)
        {
            var comboBoxColumn = new DataGridViewComboBoxColumn
            {
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Name = column.Name,
                Tag = column,
                Width = CalculateColumnWidth(definition),
                MaxDropDownItems = 2
            };
            
            comboBoxColumn.Items.AddRange("Да", "Нет");
            return comboBoxColumn;
        }

        private DataGridViewComboBoxColumn CreateEnumComboBoxColumn(TableColumn column, ColumnDefinition definition)
        {
            var comboBoxColumn = new DataGridViewComboBoxColumn
            {
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Name = column.Name,
                Tag = column,
                Width = CalculateColumnWidth(definition),
                MaxDropDownItems = TableSchema.Configuration.DefaultSettings.MaxDropDownItems
            };

            return comboBoxColumn;
        }

        private DataGridViewTextBoxColumn CreateTextBoxColumn(TableColumn column, ColumnDefinition definition)
        {
            return new DataGridViewTextBoxColumn
            {
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Name = column.Name,
                Tag = column,
                Width = CalculateColumnWidth(definition)
            };
        }

        private int CalculateColumnWidth(ColumnDefinition definition)
        {
            if (definition.Width > 0)
                return definition.Width;
                
            // Для автоматической ширины (Width = -1) вычисляем оставшееся место
            var fixedColumnsWidth = TableSchema.Configuration.Columns
                .Where(c => c.Width > 0)
                .Sum(c => c.Width);
                
            var availableWidth = _dataGridView.Width - 
                                 TableSchema.Configuration.DefaultSettings.RowHeadersWidth - 
                                 TableSchema.Configuration.DefaultSettings.ExtraMargin;
                                
            return availableWidth - fixedColumnsWidth;
        }

        private void ApplyCellAlignment()
        {
            foreach (var definition in TableSchema.Configuration.Columns)
            {
                if (definition.Index >= _dataGridView.Columns.Count) continue;
                
                var column = _dataGridView.Columns[definition.Index];
                column.DefaultCellStyle.Alignment = definition.Alignment;
                column.HeaderCell.Style.Alignment = definition.Alignment;
            }
        }

        public void SetActionColumnData(Dictionary<int, string> actionMap)
        {
            if (actionMap == null || _dataGridView.Columns.Count == 0) return;

            var actionColumn = _dataGridView.Columns[0] as DataGridViewComboBoxColumn;
            if (actionColumn == null) return;

            actionColumn.Items.Clear();
            foreach (var item in actionMap.Values)
            {
                actionColumn.Items.Add(item);
            }

            // Обновляем TableColumn с новыми данными
            if (_columns.Count > 0)
            {
                var updatedColumn = new TableColumn(_columns[0].Name, actionMap)
                {
                    GridIndex = _columns[0].GridIndex
                };
                _columns[0] = updatedColumn;
                actionColumn.Tag = updatedColumn;
            }
        }

        public void SetTargetColumnData(Dictionary<int, string> targetMap)
        {
            if (targetMap == null || _dataGridView.Columns.Count <= 1) return;

            var targetColumn = _dataGridView.Columns[1] as DataGridViewComboBoxColumn;
            if (targetColumn == null) return;

            targetColumn.Items.Clear();
            foreach (var item in targetMap.Values)
            {
                targetColumn.Items.Add(item);
            }
        }

        public TableColumn GetTableColumn(int columnIndex)
        {
            return columnIndex >= 0 && columnIndex < _columns.Count 
                ? _columns[columnIndex] 
                : null;
        }

        public bool IsComboBoxColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= _dataGridView.Columns.Count) 
                return false;

            return _dataGridView.Columns[columnIndex] is DataGridViewComboBoxColumn;
        }
    }
    
    internal class TableColumn
    {
        public TableColumn(string name, PropertyType type)
        {
            Name = name;
            Type = type;
        }

        public TableColumn(string name, Dictionary<int, string> intStringMap)
        {
            Name = name;
            Type = PropertyType.Enum;
            IntStringMap = intStringMap;
        }

        public string Name { get; }

        public PropertyType Type { get; }

        public Dictionary<int, string> IntStringMap { get; }

        public int GridIndex { get; set; }
    }
}