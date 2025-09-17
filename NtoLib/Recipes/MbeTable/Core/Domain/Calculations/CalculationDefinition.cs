#nullable enable

using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Calculations;

/// <summary>
/// Represents a parsed calculation configuration for a column.
/// </summary>
public sealed record CalculationDefinition
{
    /// <summary>
    /// Raw formula with column references in square brackets: [column-key].
    /// </summary>
    public string Formula { get; init; }

    /// <summary>
    /// Set of dependent column identifiers extracted from the formula.
    /// </summary>
    public IReadOnlyList<ColumnIdentifier> DependencyKeys { get; init; }

    public CalculationDefinition(string formula, IReadOnlyList<ColumnIdentifier> dependencyKeys)
    {
        Formula = formula ?? throw new ArgumentNullException(nameof(formula));
        if (string.IsNullOrWhiteSpace(Formula))
            throw new ArgumentNullException(nameof(formula), "Formula cannot be empty.");
        DependencyKeys = dependencyKeys ?? throw new ArgumentNullException(nameof(dependencyKeys));
    }
}