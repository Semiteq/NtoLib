using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps.Definitions
{
    public class WaitDefaults : IStepDefaultsProvider
    {
        private readonly PropertyDefinitionRegistry _registry;

        public WaitDefaults(PropertyDefinitionRegistry registry)
        {
            _registry = registry;
        }

        public Dictionary<ColumnIdentifier, StepProperty?> GetDefaultParameters()
        {
            return new Dictionary<ColumnIdentifier, StepProperty?>
            {
                [WellKnownColumns.Setpoint] = new StepProperty(60f, PropertyType.Time, _registry),
                [WellKnownColumns.Comment] = new StepProperty("", PropertyType.String, _registry)
            };
        }
    }
}