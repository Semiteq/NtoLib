namespace NtoLib.Recipes.MbeTable.RecipeManager.PropertyDataType.Definitions
{
    public class PercentDefinition : FloatDefinitionBase
    {
        public override string Units => "%";
        protected override float MinValue => 0;
        protected override float MaxValue => 100;
        protected override string MinMaxErrorMessage => $"Процент должен быть в пределах от {MinValue} до {MaxValue}";
    }
}