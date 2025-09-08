using System;
using System.Globalization;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

public class PressureDefinition : FloatDefinitionBase
{
    public override string Units => "Па";
    public override float MinValue => 0;
    public override float MaxValue => 200000;
    public override string MinMaxErrorMessage => $"Давление должно быть в пределах от {MinValue} до {MaxValue}";
}
