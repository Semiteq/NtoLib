#nullable enable

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps.Definitions
{
    public class PowerWaitDefaults : IStepDefaultsProvider
    {
        private readonly PropertyDefinitionRegistry _registry;

        public PowerWaitDefaults(PropertyDefinitionRegistry registry)
        {
            _registry = registry;
        }

        public Dictionary<ColumnKey, StepProperty?> GetDefaultParameters()
        {
            return new Dictionary<ColumnKey, StepProperty?>
            {
                [ColumnKey.ActionTarget] = null,
                [ColumnKey.Setpoint] = new StepProperty(10f, PropertyType.Percent, _registry),
                [ColumnKey.StepDuration] = new StepProperty(60f, PropertyType.Time, _registry),
                [ColumnKey.Comment] = new StepProperty("", PropertyType.String, _registry)
            };
        }
    }
}