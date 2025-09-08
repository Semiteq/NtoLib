namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions
{
    public class TempSpeedDefinition : FloatDefinitionBase
    {
        public override string Units => "°C/мин";
        public override float MinValue => -1000;
        public override float MaxValue => 1000;
        public override string MinMaxErrorMessage => $"Скорость температуры должна быть в пределах от {MinValue} до {MaxValue}";
    }
}