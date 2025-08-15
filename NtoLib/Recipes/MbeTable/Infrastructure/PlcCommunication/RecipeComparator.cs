#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PlcCommunication;

/// <summary>
/// Represents a comparator for comparing recipes consisting of multiple steps.
/// </summary>
public class RecipeComparator : IRecipeComparator
{
    private readonly DebugLogger _debugLogger;
    private const double FloatEpsilon = 1e-4;

    
    public RecipeComparator(DebugLogger debugLogger)
    {
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));    
    }

    /// <summary>
    /// Compares two recipes, each represented as a list of steps, and determines if they are equivalent.
    /// </summary>
    /// <param name="recipe1">The first recipe to compare, represented as a list of steps.</param>
    /// <param name="recipe2">The second recipe to compare, represented as a list of steps.</param>
    /// <returns>
    /// A boolean indicating whether the two recipes are equivalent.
    /// Returns false if the recipes differ in terms of step count or individual step contents.
    /// </returns>
    public bool Compare(List<Step> recipe1, List<Step> recipe2)
    {
        if (recipe1.Count != recipe2.Count)
        {
            _debugLogger.Log($"[RecipeComparator] Row count differs: {recipe1.Count} != {recipe2.Count}");
            return false;
        }

        for (var i = 0; i < recipe1.Count; i++)
        {
            if (!CompareSteps(recipe1[i], recipe2[i], i, out var reason))
            {
                _debugLogger.Log($"[RecipeComparator] Row {i} differs: {reason}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Compares two individual steps from a recipe and determines if they are equivalent based on their properties.
    /// </summary>
    /// <param name="a">The first step to compare, containing a set of properties.</param>
    /// <param name="b">The second step to compare, containing a set of properties.</param>
    /// <param name="rowIndex">The index of the current step being compared for referencing purposes.</param>
    /// <param name="reason">
    /// An output parameter that provides details about the first detected discrepancy if the steps are not equivalent.
    /// This includes the key of the differing property and the values in both steps.
    /// </param>
    /// <returns>
    /// A boolean indicating whether the two steps are equivalent.
    /// Returns false if there is any difference in the steps' property values, excluding those related to StepStartTime.
    /// </returns>
    private bool CompareSteps(Step a, Step b, int rowIndex, out string reason)
    {
        var keys = a.Properties.Keys
            .Union(b.Properties.Keys)
            .Where(k => k != ColumnKey.StepStartTime) // UI-only
            .ToArray();

        foreach (var key in keys)
        {
            a.Properties.TryGetValue(key, out var pa);
            b.Properties.TryGetValue(key, out var pb);

            var va = pa?.GetValueAsObject();
            var vb = pb?.GetValueAsObject();

            if (!ValueEquals(va, vb))
            {
                reason = $"Key={key}, A='{Format(va)}', B='{Format(vb)}'";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    private string Format(object? v) => v switch
    {
        null => "null",
        float f => f.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
        double d => d.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
        _ => v?.ToString() ?? "null"
    };

    private bool ValueEquals(object? a, object? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        if (TryToDouble(a, out var da) && TryToDouble(b, out var db))
            return Math.Abs(da - db) <= FloatEpsilon;

        if (a is string sa && b is string sb)
            return string.Equals(sa.Trim(), sb.Trim(), StringComparison.Ordinal);

        return Equals(a, b);
    }

    private bool TryToDouble(object v, out double d)
    {
        switch (v)
        {
            case float f:
                d = f;
                return true;
            case double dd:
                d = dd;
                return true;
            case int i:
                d = i;
                return true;
            case long l:
                d = l;
                return true;
            case string s when double.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed):
                d = parsed;
                return true;
            default:
                d = 0;
                return false;
        }
    }
}