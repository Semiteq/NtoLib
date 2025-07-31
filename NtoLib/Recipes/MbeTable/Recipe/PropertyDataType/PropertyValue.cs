using OneOf;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;

public class PropertyValue
{
    public readonly OneOf<bool, int, float, string> UnionValue;
    public readonly PropertyType Type;
    
    public PropertyValue(OneOf<bool, int, float, string> unionValue, PropertyType type)
    {
        UnionValue = unionValue;
        Type = type;
    }
}