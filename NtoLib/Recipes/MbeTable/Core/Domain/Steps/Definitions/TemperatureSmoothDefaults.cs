#nullable enable

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps.Definitions
{
    public class TemperatureSmoothDefaults : IStepDefaultsProvider
    {
        private readonly PropertyDefinitionRegistry _registry;

        public TemperatureSmoothDefaults(PropertyDefinitionRegistry registry)
        {
            _registry = registry;
        }

        public Dictionary<ColumnIdentifier, StepProperty?> GetDefaultParameters()
        {
            return new Dictionary<ColumnIdentifier, StepProperty?>
            {
                [WellKnownColumns.ActionTarget] = null,
                [WellKnownColumns.InitialValue] = new StepProperty(500f, PropertyType.Temp, _registry),
                [WellKnownColumns.Setpoint] = new StepProperty(600f, PropertyType.Temp, _registry),
                [WellKnownColumns.Speed] = new StepProperty(10f, PropertyType.TempSpeed, _registry),
                [WellKnownColumns.StepDuration] = new StepProperty(600f, PropertyType.Time, _registry),
                [WellKnownColumns.Comment] = new StepProperty("", PropertyType.String, _registry)
            };
        }
    }
}