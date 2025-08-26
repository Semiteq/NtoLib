#nullable enable

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps.Definitions
{
    public class PowerSmoothDefaults : IStepDefaultsProvider
    {
        private readonly PropertyDefinitionRegistry _registry;

        public PowerSmoothDefaults(PropertyDefinitionRegistry registry)
        {
            _registry = registry;
        }

        public Dictionary<ColumnIdentifier, StepProperty?> GetDefaultParameters()
        {
            return new Dictionary<ColumnIdentifier, StepProperty?>
            {
                [WellKnownColumns.ActionTarget] = null,
                [WellKnownColumns.InitialValue] = new StepProperty(10f, PropertyType.Percent, _registry),
                [WellKnownColumns.Setpoint] = new StepProperty(20f, PropertyType.Percent, _registry),
                [WellKnownColumns.Speed] = new StepProperty(1f, PropertyType.PowerSpeed, _registry),
                [WellKnownColumns.StepDuration] = new StepProperty(600f, PropertyType.Time, _registry),
                [WellKnownColumns.Comment] = new StepProperty("", PropertyType.String, _registry)
            };
        }
    }
}