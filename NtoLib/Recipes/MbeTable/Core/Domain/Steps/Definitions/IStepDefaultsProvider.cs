using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps.Definitions
{
    public interface IStepDefaultsProvider
    {
        Dictionary<ColumnKey, StepProperty> GetDefaultParameters();
    }
}