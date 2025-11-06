using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;

public sealed record ActionDefinition(
    short Id,
    string Name,
    IReadOnlyList<PropertyConfig> Columns,
    DeployDuration DeployDuration,
    FormulaDefinition? Formula
);