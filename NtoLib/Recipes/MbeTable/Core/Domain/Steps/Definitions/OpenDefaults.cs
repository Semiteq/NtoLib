using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps.Definitions
{
    public class OpenDefaults : IStepDefaultsProvider
    {
        private readonly PropertyDefinitionRegistry _registry;

        public OpenDefaults(PropertyDefinitionRegistry registry)
        {
            _registry = registry;
        }

        public Dictionary<ColumnKey, StepProperty> GetDefaultParameters()
        {
            return new Dictionary<ColumnKey, StepProperty>
            {
                [ColumnKey.Comment] = new StepProperty("", PropertyType.String, _registry)
            };
        }
    }
}