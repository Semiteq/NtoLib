#nullable enable
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns;

public abstract class BaseRecipeComboBoxColumn : DataGridViewComboBoxColumn
{
    protected BaseRecipeComboBoxColumn()
    {
        FlatStyle = FlatStyle.Flat;
        DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
        DisplayStyleForCurrentCellOnly = true;
        ValueType = typeof(int?);
    }
}
