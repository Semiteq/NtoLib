#nullable enable

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps.Definitions
{
    public class OpenTimeDefaults : IStepDefaultsProvider
    {
        private readonly PropertyDefinitionRegistry _registry;

        public OpenTimeDefaults(PropertyDefinitionRegistry registry)
        {
            _registry = registry;
        }

        public Dictionary<ColumnIdentifier, StepProperty?> GetDefaultParameters()
        {
            return new Dictionary<ColumnIdentifier, StepProperty?>
            {
                [WellKnownColumns.ActionTarget] = null,
                [WellKnownColumns.Setpoint] = new StepProperty(1f, PropertyType.Time, _registry),
                [WellKnownColumns.Comment] = new StepProperty("", PropertyType.String, _registry)
            };
        }
    }
}