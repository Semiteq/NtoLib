using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Table;

public class TableCellFormatter
{
    public string GetFormattedValue(DynamicStepViewModel viewModel, ColumnKey columnKey)
    {
        return viewModel.GetFormattedValue(columnKey); 
    }
}