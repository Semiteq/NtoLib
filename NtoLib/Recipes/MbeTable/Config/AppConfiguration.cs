#nullable enable

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.PinGroups;
using NtoLib.Recipes.MbeTable.Core.Domain.Calculations;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;

namespace NtoLib.Recipes.MbeTable.Config;

public sealed record AppConfiguration(
    PropertyDefinitionRegistry PropertyRegistry,
    TableColumns Columns,
    IReadOnlyDictionary<int, ActionDefinition> Actions,
    IReadOnlyCollection<PinGroupData> PinGroupData,
    ICalculationOrderer CalculationOrderer
);