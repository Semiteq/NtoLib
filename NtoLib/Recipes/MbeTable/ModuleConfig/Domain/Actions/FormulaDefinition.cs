using System;
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;

public sealed record FormulaDefinition
{
    public string Expression { get; init; } = string.Empty;

    public IReadOnlyList<string> RecalcOrder { get; init; } = Array.Empty<string>();
}