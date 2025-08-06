namespace NtoLib.Recipes.MbeTable.RecipeManager.PropertyDataType.Definitions
{
    public class FlowDefinition : FloatDefinitionBase
    {
        public override string Units => "см³/мин";
        protected override float MinValue => 0;
        protected override float MaxValue => 100000;
        protected override string MinMaxErrorMessage => $"Поток должен быть в пределах от {MinValue} до {MaxValue}";
    }
}