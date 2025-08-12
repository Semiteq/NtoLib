namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions
{
    public class PowerSpeedDefinition : FloatDefinitionBase
    {
        public override string Units => "%/мин";
        public override float MinValue => -100;
        public override float MaxValue => 100;
        public override string MinMaxErrorMessage => $"Скорость мощности должна быть в пределах от {MinValue} до {MaxValue}";
    }
}