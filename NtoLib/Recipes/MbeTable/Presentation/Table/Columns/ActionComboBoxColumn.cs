#nullable enable
using NtoLib.Recipes.MbeTable.Presentation.Table.Cells;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns;

public sealed class ActionComboBoxColumn : BaseRecipeComboBoxColumn
{
    public ActionComboBoxColumn()
    {
        CellTemplate = new ActionComboBoxCell();
    }
}