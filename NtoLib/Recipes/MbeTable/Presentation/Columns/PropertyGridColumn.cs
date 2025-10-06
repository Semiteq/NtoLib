using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Presentation.Cells;

namespace NtoLib.Recipes.MbeTable.Presentation.Columns;

/// <summary>
/// A custom DataGridView column for PropertyGridCell. The purpose is to allow text
/// input for units while a cell type is float or int.
/// </summary>
public sealed class PropertyGridColumn : DataGridViewTextBoxColumn
{
    public PropertyGridColumn()
    {
        CellTemplate = new PropertyGridCell();
    }
}