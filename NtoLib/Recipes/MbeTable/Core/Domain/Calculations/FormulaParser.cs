#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Calculations;

/// <summary>
/// Simple formula parser for patterns like [column-key].
/// </summary>
public sealed class FormulaParser : IFormulaParser
{
    private static readonly Regex Bracketed = new Regex(@"\[(?<k>[a-zA-Z0-9_\-]+)\]", RegexOptions.Compiled);

    public IReadOnlyList<ColumnIdentifier> GetDependencies(string formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
            throw new ArgumentNullException(nameof(formula));

        // Use dictionary for uniqueness (case-insensitive), preserve insertion order via auxiliary list.
        var known = new Dictionary<string, ColumnIdentifier>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<ColumnIdentifier>();

        var matches = Bracketed.Matches(formula);
        for (int i = 0; i < matches.Count; i++)
        {
            var m = matches[i];
            var group = m.Groups["k"];
            if (!group.Success) continue;

            var raw = group.Value;
            if (raw.Length == 0) continue;

            if (!known.ContainsKey(raw))
            {
                var id = new ColumnIdentifier(raw);
                known.Add(raw, id);
                ordered.Add(id);
            }
        }

        return ordered.AsReadOnly();
    }

    public string ConvertFormulaToDataTableSyntax(
        string formula,
        IReadOnlyDictionary<ColumnIdentifier, string> columnKeyToDataTableNameMap)
    {
        if (string.IsNullOrWhiteSpace(formula))
            throw new ArgumentNullException(nameof(formula));
        if (columnKeyToDataTableNameMap == null)
            throw new ArgumentNullException(nameof(columnKeyToDataTableNameMap));

        // Replace each [key] using regex evaluator.
        return Bracketed.Replace(formula, match =>
        {
            var g = match.Groups["k"];
            if (!g.Success)
                return match.Value;

            var raw = g.Value;
            var lookupId = new ColumnIdentifier(raw);

            string? mapped = null;
            foreach (var kv in columnKeyToDataTableNameMap)
            {
                if (string.Equals(kv.Key.Value, lookupId.Value, StringComparison.OrdinalIgnoreCase))
                {
                    mapped = kv.Value;
                    break;
                }
            }

            if (mapped == null)
                throw new KeyNotFoundException($"Unknown column reference '{raw}' in formula.");

            return mapped;
        });
    }
}