#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Calculations;

public sealed class CalculationOrderer : ICalculationOrderer
{
    private readonly ILogger _logger;
    private readonly IFormulaParser _parser;

    private bool _initialized;
    private List<ColumnDefinition>? _ordered;
    private Dictionary<ColumnIdentifier, string>? _map;

    public CalculationOrderer(ILogger logger, IFormulaParser parser)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    public Result<IReadOnlyList<ColumnDefinition>> OrderAndValidate(IReadOnlyList<ColumnDefinition> allColumns)
    {
        if (allColumns == null)
            return Result.Fail("Columns collection is null.");

        if (_initialized)
            return Result.Fail("CalculationOrderer already initialized.");

        var calculated = allColumns
            .Where(c => c.Calculation is not null)
            .ToList();

        // Map for DataTable column names
        _map = allColumns
            .ToDictionary(
                c => c.Key,
                c => SanitizeName(c.Key.Value),
                new ColumnIdentifierComparer());

        // Build adjacency list
        var adj = new Dictionary<ColumnIdentifier, List<ColumnIdentifier>>(new ColumnIdentifierComparer());
        foreach (var c in calculated)
        {
            var deps = c.Calculation!.DependencyKeys;
            adj[c.Key] = deps.ToList();
        }

        // Detect cycles + topological sort (DFS)
        var temp = new HashSet<ColumnIdentifier>(new ColumnIdentifierComparer());
        var perm = new HashSet<ColumnIdentifier>(new ColumnIdentifierComparer());
        var result = new List<ColumnDefinition>();
        var cycleStack = new Stack<ColumnIdentifier>();

        bool Visit(ColumnIdentifier k)
        {
            if (perm.Contains(k)) return true;
            if (temp.Contains(k))
            {
                var cycle = cycleStack.Reverse().SkipWhile(x => !Equals(x, k)).Append(k).ToList();
                var error = new GraphCycleError("Cycle detected in calculated columns.", cycle);
                return false;
            }

            temp.Add(k);
            cycleStack.Push(k);

            if (adj.TryGetValue(k, out var deps))
            {
                foreach (var d in deps)
                {
                    if (!adj.ContainsKey(d))
                    {
                        // dependency may be an input (non-calculated) — ok
                        if (calculated.All(c2 => c2.Key != d))
                            continue;
                    }

                    if (!Visit(d))
                        return false;
                }
            }

            cycleStack.Pop();
            temp.Remove(k);
            perm.Add(k);
            var def = calculated.First(c => c.Key == k);
            result.Add(def);
            return true;
        }

        foreach (var c in calculated)
        {
            if (!perm.Contains(c.Key))
            {
                if (!Visit(c.Key))
                {
                    return Result.Fail<IReadOnlyList<ColumnDefinition>>("Cycle detected (see logs).");
                }
            }
        }

        // Reverse post-order for topological sort
        result.Reverse();
        _ordered = result;
        _initialized = true;

        _logger.Log($"Calculation order established: {string.Join(", ", _ordered.Select(o => o.Key.Value))}");
        return Result.Ok<IReadOnlyList<ColumnDefinition>>(_ordered);
    }

    public IReadOnlyList<ColumnDefinition> GetCalculatedColumnsInOrder()
    {
        if (!_initialized || _ordered == null)
            throw new InvalidOperationException("OrderAndValidate not called.");
        return _ordered;
    }

    public IReadOnlyDictionary<ColumnIdentifier, string> GetColumnKeyToDataTableNameMap()
    {
        if (!_initialized || _map == null)
            throw new InvalidOperationException("OrderAndValidate not called.");
        return _map;
    }

    private static string SanitizeName(string raw)
    {
        // Allowed: letters, digits, underscore. Replace others with underscore.
        var chars = raw.Select(c =>
            char.IsLetterOrDigit(c) ? c :
            c == '_' ? '_' :
            '_').ToArray();
        return new string(chars);
    }

    private sealed class ColumnIdentifierComparer : IEqualityComparer<ColumnIdentifier>
    {
        public bool Equals(ColumnIdentifier? x, ColumnIdentifier? y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x?.Value, y?.Value);

        public int GetHashCode(ColumnIdentifier obj) =>
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Value);
    }
}