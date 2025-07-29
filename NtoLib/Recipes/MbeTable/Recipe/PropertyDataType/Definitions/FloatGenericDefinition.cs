namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType.Definitions;

public class FloatGenericDefinition : FloatDefinitionBase
{
    public override string Units => "";
    protected override float MinValue => float.MinValue;
    protected override float MaxValue => float.MaxValue;
    protected override string MinMaxErrorMessage => "Значение выходит за пределы допустимого диапазона для float.";
}