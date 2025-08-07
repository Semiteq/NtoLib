#nullable enable

using System.Collections.Generic;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Composition;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns;

public class ActionTargetComboBoxColumnFactory  : IColumnFactory
{
    public DataGridViewColumn CreateColumn(ColumnDefinition colDef, ColorScheme colorScheme)
    {
        var dataSource = new List<KeyValuePair<int, string>> { new(0, "тест0"), new(1, "тест1"), new(2, "тест2") };
        var comboColumn = new DataGridViewComboBoxColumn
        {
            Name = colDef.Key.ToString(),
            HeaderText = colDef.UiName,
            DataPropertyName = nameof(StepViewModel.ActionTarget),
            DataSource = dataSource,
            DisplayMember = "Value",
            ValueMember = "Key",
            ValueType = typeof(int),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
        };

        comboColumn.DefaultCellStyle.Alignment = colDef.Alignment;
        comboColumn.DefaultCellStyle.Font = colorScheme.LineFont;
        comboColumn.DefaultCellStyle.BackColor = colorScheme.LineBgColor;
        comboColumn.DefaultCellStyle.Font = colorScheme.LineFont;
        
        if (colDef.Width > 0)
        {
            comboColumn.Width = colDef.Width;
            comboColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        }
        else
        {
            comboColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }
        
        return comboColumn;
    }
}