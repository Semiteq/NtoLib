namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType.Definitions
{
    public class TimeDefinition : FloatDefinitionBase
    {
        public override string Units => "с";
        protected override float MinValue => 0;
        protected override float MaxValue => 86400;
        protected override string MinMaxErrorMessage => $"Время должно быть в пределах от {MinValue} до {MaxValue}";
    }
}