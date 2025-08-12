namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions
{
    public class PercentDefinition : FloatDefinitionBase
    {
        public override string Units => "%";
        public override float MinValue => 0;
        public override float MaxValue => 100;
        public override string MinMaxErrorMessage => $"Процент должен быть в пределах от {MinValue} до {MaxValue}";
    }
}