#nullable enable

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps.Definitions
{
    public class CloseDefaults : IStepDefaultsProvider
    {
        private readonly PropertyDefinitionRegistry _registry;

        public CloseDefaults(PropertyDefinitionRegistry registry)
        {
            _registry = registry;
        }

        public Dictionary<ColumnIdentifier, StepProperty?> GetDefaultParameters()
        {
            return new Dictionary<ColumnIdentifier, StepProperty?>
            {
                [WellKnownColumns.ActionTarget] = null,
                [WellKnownColumns.Comment] = new StepProperty("", PropertyType.String, _registry)
            };
        }
    }
}