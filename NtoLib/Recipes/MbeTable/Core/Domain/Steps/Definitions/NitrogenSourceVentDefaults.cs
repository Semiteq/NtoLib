using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps.Definitions
{
    public class NitrogenSourceVentDefaults : IStepDefaultsProvider
    {
        private readonly PropertyDefinitionRegistry _registry;

        public NitrogenSourceVentDefaults(PropertyDefinitionRegistry registry)
        {
            _registry = registry;
        }

        public Dictionary<ColumnKey, StepProperty> GetDefaultParameters()
        {
            return new Dictionary<ColumnKey, StepProperty>
            {
                [ColumnKey.Setpoint] = new StepProperty(10f, PropertyType.Flow, _registry),
                [ColumnKey.Comment] = new StepProperty("", PropertyType.String, _registry)
            };
        }
    }
}