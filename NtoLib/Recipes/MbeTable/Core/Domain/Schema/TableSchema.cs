using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Schema
{
    public class TableSchema
    {
        private readonly List<ColumnDefinition> _columns = new()
        {
            new ColumnDefinition
            {
                Key = ColumnKey.Action,
                Index = 0,
                UiName = "Действие",
                PropertyType = PropertyType.Enum,
                Type = typeof(int),
                TableCellType = typeof(DataGridViewComboBoxCell),
                Width = 200,
                ReadOnly = false,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
            },
            new ColumnDefinition
            {
                Key = ColumnKey.ActionTarget,
                Index = 1,
                UiName = "Объект",
                PropertyType = PropertyType.Enum,
                TableCellType = typeof(DataGridViewComboBoxCell),
                Type = typeof(int),
                Width = 150,
                ReadOnly = false,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
            },
            new ColumnDefinition
            {
                Key = ColumnKey.InitialValue,
                Index = 2,
                UiName = "Нач.значение",
                PropertyType = PropertyType.Float,
                TableCellType = typeof(DataGridViewTextBoxCell),
                Type = typeof(float),
                Width = 200,
                ReadOnly = false,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
            },
            new ColumnDefinition
            {
                Key = ColumnKey.Setpoint,
                Index = 3,
                UiName = "Задание",
                PropertyType = PropertyType.Float,
                TableCellType = typeof(DataGridViewTextBoxCell),
                Type = typeof(float),
                Width = 180,
                ReadOnly = false,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
            },
            new ColumnDefinition
            {
                Key = ColumnKey.Speed,
                Index = 4,
                UiName = "Скорость",
                PropertyType = PropertyType.Float,
                TableCellType = typeof(DataGridViewTextBoxCell),
                Type = typeof(float),
                Width = 150,
                ReadOnly = false,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
            },
            new ColumnDefinition
            {
                Key = ColumnKey.StepDuration,
                Index = 5,
                UiName = "Длительность",
                PropertyType = PropertyType.Float,
                TableCellType = typeof(DataGridViewTextBoxCell),
                Type = typeof(float),
                Width = 200,
                ReadOnly = false,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
            },
            new ColumnDefinition
            {
                Key = ColumnKey.StepStartTime,
                Index = 6,
                UiName = "Время",
                PropertyType = PropertyType.Float,
                TableCellType = typeof(DataGridViewTextBoxCell),
                Type = typeof(float),
                Width = 150,
                ReadOnly = true,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
            },
            new ColumnDefinition
            {
                Key = ColumnKey.Comment,
                Index = 7,
                UiName = "Комментарий",
                PropertyType = PropertyType.String,
                TableCellType = typeof(DataGridViewTextBoxCell),
                Type = typeof(string),
                Width = -1,
                ReadOnly = false,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
            }
        };

        public int GetColumnCount() => _columns.Count;

        public IReadOnlyList<ColumnDefinition> GetColumns() => _columns.AsReadOnly();

        public Type GetColumnTypeByColumnKey(ColumnKey key)
        {
            var column = _columns.FirstOrDefault(c => c.Key == key);
            if (column == null)
                throw new ArgumentException($"Column with key {key} does not exist.");
            return column.Type;
        }

        public string GetColumnUiNameByKey(ColumnKey key)
        {
            var column = _columns.FirstOrDefault(c => c.Key == key);
            if (column == null)
                throw new ArgumentException($"Column with key {key} does not exist.");
            return column.UiName;
        }

        public ColumnKey GetColumnKeyByIndex(int index)
        {
            if (index < 0 || index >= _columns.Count)
                throw new IndexOutOfRangeException("Invalid column index.");
            return _columns[index].Key;
        }

        public int GetIndexByColumnKey(ColumnKey key)
        {
            var column = _columns.FirstOrDefault(c => c.Key == key);
            if (column == null)
                throw new ArgumentException($"Column with key {key} does not exist.");
            return column.Index;
        }

        public ColumnDefinition GetColumnDefinition(int index)
        {
            if (index < 0 || index >= _columns.Count)
                throw new IndexOutOfRangeException("Invalid column index.");

            if (_columns[index] == null)
                throw new ArgumentException($"Column at index {index} is null.");

            return _columns[index];
        }

        public ColumnDefinition GetColumnDefinition(ColumnKey key)
        {
            var column = _columns.FirstOrDefault(c => c.Key == key);
            if (column == null)
                throw new ArgumentException($"Column with key {key} does not exist.");
            return column;
        }
    }
}