using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Schema
{
    public class TableSchema
    {
        private readonly List<ColumnDefinition> _columns = new()
        {
            new ColumnDefinition
            (
                ColumnKey.Action,
                0,
                "action",
                "Действие",
                typeof(int),
                200,
                false,
                DataGridViewContentAlignment.MiddleLeft
            ),
            new ColumnDefinition
            (
                ColumnKey.ActionTarget,
                1,
                "action-target",
                "Объект",
                typeof(int),
                150,
                false,
                DataGridViewContentAlignment.MiddleCenter
            ),
            new ColumnDefinition
            (
                ColumnKey.InitialValue,
                2,
                "initial-value",
                "Нач.значение",
                typeof(float),
                200,
                false,
                DataGridViewContentAlignment.MiddleCenter
            ),
            new ColumnDefinition
            (
                ColumnKey.Setpoint,
                3,
                "setpoint",
                "Задание",
                typeof(float),
                180,
                false,
                DataGridViewContentAlignment.MiddleCenter
            ),
            new ColumnDefinition
            (
                ColumnKey.Speed,
                4,
                "speed",
                "Скорость",
                typeof(float),
                150,
                false,
                DataGridViewContentAlignment.MiddleCenter
            ),
            new ColumnDefinition
            (
                ColumnKey.StepDuration,
                5,
                "step-duration",
                "Длительность",
                typeof(float),
                200,
                false,
                DataGridViewContentAlignment.MiddleCenter
            ),
            new ColumnDefinition
            (
                ColumnKey.StepStartTime,
                6,
                "step-start-time",
                "Время",
                typeof(float),
                150,
                true,
                DataGridViewContentAlignment.MiddleCenter
            ),
            new ColumnDefinition
            (
                ColumnKey.Comment,
                7,
                "comment",
                "Комментарий",
                typeof(string),
                -1,
                false,
                DataGridViewContentAlignment.MiddleCenter
            )
        };
        
        public IReadOnlyList<ColumnDefinition>  GetColumns() => _columns.AsReadOnly();

        public ColumnDefinition GetColumnDefinition(int index)
        {
            if (index < 0 || index >= _columns.Count)
                throw new IndexOutOfRangeException("Invalid column index.");

            if (_columns[index] == null)
                throw new ArgumentException($"Column at index {index} is null.");

            return _columns[index];
        }
    }
}