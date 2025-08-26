#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Utils;

public sealed class RecipeComparatorV1 : IRecipeComparator
{
    private readonly DebugLogger _debugLogger;
    private readonly ICommunicationSettingsProvider _communicationSettingsProvider;

    public RecipeComparatorV1(DebugLogger debugLogger, ICommunicationSettingsProvider communicationSettingsProvider)
    {
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
        _communicationSettingsProvider = communicationSettingsProvider ?? throw new ArgumentNullException(nameof(communicationSettingsProvider));
    }

    private CommunicationSettings Settings => _communicationSettingsProvider.GetSettings();
    
    public Result Compare(Recipe recipe1, Recipe recipe2)
    {
        if (recipe1.Steps.Count != recipe2.Steps.Count)
        {
            var error = $"Row count differs: {recipe1.Steps.Count} != {recipe2.Steps.Count}";
            _debugLogger.Log(error);
            return Result.Fail(error);
        }

        for (var i = 0; i < recipe1.Steps.Count; i++)
        {
            var compareResult = CompareSteps(recipe1.Steps[i], recipe2.Steps[i]);
            if (compareResult.IsFailed)
            {
                var error = $"Row {i} differs: {string.Join("; ", compareResult.Errors.Select(e => e.Message))}";
                _debugLogger.Log(error);
                return Result.Fail(error);
            }
        }

        return Result.Ok();
    }

    public Result CompareSteps(Step a, Step b)
    {
        var excluded = new HashSet<ColumnIdentifier> { WellKnownColumns.StepStartTime, WellKnownColumns.Comment };
        var keys = a.Properties.Keys
            .Union(b.Properties.Keys)
            .Where(k => !excluded.Contains(k))
            .ToArray();

        foreach (var key in keys)
        {
            a.Properties.TryGetValue(key, out var pa);
            b.Properties.TryGetValue(key, out var pb);

            var va = pa?.GetValueAsObject();
            var vb = pb?.GetValueAsObject();

            if (!ValueEquals(va, vb))
            {
                return Result.Fail($"Key={key.Value}, A='{Format(va)}', B='{Format(vb)}'");
            }
        }

        return Result.Ok();
    }

    public string Format(object? v) => v switch
    {
        null => "null",
        float f => f.ToString("R", CultureInfo.InvariantCulture),
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        _ => v?.ToString() ?? "null"
    };

    public bool ValueEquals(object? a, object? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        if (TryToDouble(a, out var da) && TryToDouble(b, out var db))
            return Math.Abs(da - db) <= Math.Abs(Settings.Epsilon);

        if (a is string sa && b is string sb)
            return string.Equals(sa.Trim(), sb.Trim(), StringComparison.Ordinal);

        return Equals(a, b);
    }

    public bool TryToDouble(object v, out double d)
    {
        switch (v)
        {
            case float f:
                d = f; return true;
            case double dd:
                d = dd; return true;
            case int i:
                d = i; return true;
            case long l:
                d = l; return true;
            case string s when double.TryParse(s, System.Globalization.NumberStyles.Float,
                CultureInfo.InvariantCulture, out var parsed):
                d = parsed; return true;
            default:
                d = 0; return false;
        }
    }
}