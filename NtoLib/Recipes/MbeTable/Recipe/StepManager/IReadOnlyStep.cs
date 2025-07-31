using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager;

public interface IReadOnlyStep
{
    IReadOnlyDictionary<ColumnKey, PropertyWrapper> ReadOnlyStep { get; }
        
    PropertyWrapper GetProperty(ColumnKey columnKey);
}