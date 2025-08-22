#nullable enable

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps.Definitions
{
    public class NitrogenRunSourceDefaults : IStepDefaultsProvider
    {
        private readonly PropertyDefinitionRegistry _registry;

        public NitrogenRunSourceDefaults(PropertyDefinitionRegistry registry)
        {
            _registry = registry;
        }

        public Dictionary<ColumnKey, StepProperty?> GetDefaultParameters()
        {
            return new Dictionary<ColumnKey, StepProperty?>
            {
                [ColumnKey.ActionTarget] = null,
                [ColumnKey.Setpoint] = new StepProperty(10f, PropertyType.Flow, _registry),
                [ColumnKey.Comment] = new StepProperty("", PropertyType.String, _registry)
            };
        }
    }
}