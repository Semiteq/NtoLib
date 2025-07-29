using OneOf;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;

public class PropertyValue
{
    public OneOf<bool, int, float, string> UnionValue;
    public readonly PropertyType Type;
    public readonly bool IsBlocked;
    
    public PropertyValue(OneOf<bool, int, float, string> unionValue, PropertyType type, bool isBlocked = false)
    {
        UnionValue = unionValue;
        Type = type;
        IsBlocked = isBlocked;
    }
    
    public bool AsBool => UnionValue.AsT0;
    public int AsInt => UnionValue.AsT1;
    public float AsFloat => UnionValue.AsT2;
    public string AsString => UnionValue.AsT3;
    
    public bool IsBool => UnionValue.IsT0;
    public bool IsInt => UnionValue.IsT1;
    public bool IsFloat => UnionValue.IsT2;
    public bool IsString => UnionValue.IsT3;
}