#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Errors;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Calculations;

public sealed class GraphCycleError : RecipeError
{
    public IReadOnlyList<ColumnIdentifier> Cycle { get; }

    public GraphCycleError(string message, IReadOnlyList<ColumnIdentifier> cycle)
        : base(message, RecipeErrorCodes.ConfigInvalidSchema)
    {
        Cycle = cycle ?? throw new ArgumentNullException(nameof(cycle));
        WithMetadata(nameof(Cycle), string.Join(" -> ", cycle.Select(c => c.Value)));
    }
}