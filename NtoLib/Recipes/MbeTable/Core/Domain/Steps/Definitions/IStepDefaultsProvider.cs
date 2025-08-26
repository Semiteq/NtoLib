#nullable enable
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps.Definitions
{
    public interface IStepDefaultsProvider
    {
        Dictionary<ColumnIdentifier, StepProperty?> GetDefaultParameters();
    }
}