using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties;

public class PropertyDefinitionRegistry
{
    private readonly IReadOnlyDictionary<PropertyType, IPropertyTypeDefinition> _definitions = new Dictionary<PropertyType, IPropertyTypeDefinition>
    {
        { PropertyType.Bool, new BoolDefinition() },
        { PropertyType.Enum, new EnumDefinition() },
        { PropertyType.Float, new FloatGenericDefinition() },
        { PropertyType.Flow, new FlowDefinition() },
        { PropertyType.Int, new IntDefinition() },
        { PropertyType.Percent, new PercentDefinition() },
        { PropertyType.PowerSpeed, new PowerSpeedDefinition() },
        { PropertyType.String, new StringDefinition() },
        { PropertyType.Temp, new TemperatureDefinition() },
        { PropertyType.TempSpeed, new TempSpeedDefinition() },
        { PropertyType.Time, new TimeDefinition() }
    };

    public IPropertyTypeDefinition GetDefinition(PropertyType type)
    {
        if (_definitions.TryGetValue(type, out var definition))
        {
            return definition;
        }
        throw new KeyNotFoundException($"No definition registered for PropertyType: {type}");
    }
}