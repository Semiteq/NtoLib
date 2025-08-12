using System.Globalization;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

public class FloatGenericDefinition : FloatDefinitionBase
{
    public override string Units => "";
    public override float MinValue => float.MinValue;
    public override float MaxValue => float.MaxValue;
    public override string MinMaxErrorMessage => "Значение выходит за пределы допустимого диапазона для float.";
    public override string FormatValue(object value) => ((float)value).ToString(CultureInfo.InvariantCulture);
}