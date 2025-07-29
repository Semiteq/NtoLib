namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType.Definitions
{
    public class PowerSpeedDefinition : FloatDefinitionBase
    {
        public override string Units => "Вт/мин";
        protected override float MinValue => -100;
        protected override float MaxValue => 100;
        protected override string MinMaxErrorMessage => $"Скорость мощности должна быть в пределах от {MinValue} до {MaxValue}";
    }
}