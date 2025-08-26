#nullable enable

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps.Definitions
{
    public class TemperatureDefaults : IStepDefaultsProvider
    {
        private readonly PropertyDefinitionRegistry _registry;

        public TemperatureDefaults(PropertyDefinitionRegistry registry)
        {
            _registry = registry;
        }

        public Dictionary<ColumnIdentifier, StepProperty?> GetDefaultParameters()
        {
            return new Dictionary<ColumnIdentifier, StepProperty?>
            {
                [WellKnownColumns.ActionTarget] = null,
                [WellKnownColumns.Setpoint] = new StepProperty(500f, PropertyType.Temp, _registry),
                [WellKnownColumns.Comment] = new StepProperty("", PropertyType.String, _registry)
            };
        }
    }
}