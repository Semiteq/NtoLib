namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType.Definitions
{
    public class TempSpeedDefinition : FloatDefinitionBase
    {
        public override string Units => "°C/мин";
        protected override float MinValue => -1000;
        protected override float MaxValue => 1000;
        protected override string MinMaxErrorMessage => $"Скорость температуры должна быть в пределах от {MinValue} до {MaxValue}";
    }
}