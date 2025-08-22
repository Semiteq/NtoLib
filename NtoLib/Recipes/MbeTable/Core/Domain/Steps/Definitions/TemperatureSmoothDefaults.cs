#nullable enable

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps.Definitions
{
    public class TemperatureSmoothDefaults : IStepDefaultsProvider
    {
        private readonly PropertyDefinitionRegistry _registry;

        public TemperatureSmoothDefaults(PropertyDefinitionRegistry registry)
        {
            _registry = registry;
        }

        public Dictionary<ColumnKey, StepProperty?> GetDefaultParameters()
        {
            return new Dictionary<ColumnKey, StepProperty?>
            {
                [ColumnKey.ActionTarget] = null,
                [ColumnKey.InitialValue] = new StepProperty(500f, PropertyType.Temp, _registry),
                [ColumnKey.Setpoint] = new StepProperty(600f, PropertyType.Temp, _registry),
                [ColumnKey.Speed] = new StepProperty(10f, PropertyType.TempSpeed, _registry),
                [ColumnKey.StepDuration] = new StepProperty(600f, PropertyType.Time, _registry),
                [ColumnKey.Comment] = new StepProperty("", PropertyType.String, _registry)
            };
        }
    }
}