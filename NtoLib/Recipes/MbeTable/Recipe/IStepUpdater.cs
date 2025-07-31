using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe;

public interface IStepUpdater
{
    bool TrySetStepPropertyByObject(int rowIndex, ColumnKey columnKey, object value, out string errorString);
}