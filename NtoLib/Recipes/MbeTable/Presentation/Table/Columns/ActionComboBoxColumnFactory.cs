#nullable enable

using System.Collections.Generic;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Composition;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns;

public class ActionComboBoxColumnFactory : IColumnFactory
{
    private readonly ComboboxDataProvider _dataProvider;

    public ActionComboBoxColumnFactory(ComboboxDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public DataGridViewColumn CreateColumn(ColumnDefinition colDef, ColorScheme colorScheme)
    {
        var comboColumn = new DataGridViewComboBoxColumn
        {
            Name = colDef.Key.ToString(),
            HeaderText = colDef.UiName,
            DataPropertyName = nameof(StepViewModel.Action),
            DataSource = _dataProvider.GetActions(),
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