#nullable enable
using NtoLib.Recipes.MbeTable.Presentation.Table.Cells;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns;

public sealed class ActionTargetComboBoxColumn : BaseRecipeComboBoxColumn
{
    public ActionTargetComboBoxColumn()
    {
        CellTemplate = new ActionTargetComboBoxCell();
    }
}