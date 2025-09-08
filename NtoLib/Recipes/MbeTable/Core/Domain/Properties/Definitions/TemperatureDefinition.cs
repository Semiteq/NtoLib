using System;
using System.Globalization;
using System.Linq;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

public class TemperatureDefinition : FloatDefinitionBase
{
    public override string Units => "°C";
    public override float MinValue => 0;
    public override float MaxValue => 2000;
    public override string MinMaxErrorMessage => $"Температура должна быть в пределах от {MinValue} до {MaxValue}";
}