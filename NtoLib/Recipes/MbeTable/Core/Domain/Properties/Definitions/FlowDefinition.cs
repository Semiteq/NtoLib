namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions
{
    public class FlowDefinition : FloatDefinitionBase
    {
        public override string Units => "см³/мин";
        public override float MinValue => 0;
        public override float MaxValue => 100000;
        public override string MinMaxErrorMessage => $"Поток должен быть в пределах от {MinValue} до {MaxValue}";
    }
}