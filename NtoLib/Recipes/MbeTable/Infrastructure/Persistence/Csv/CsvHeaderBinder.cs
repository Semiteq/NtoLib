#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv;

/// <summary>
/// A utility class to bind CSV file headers to a specified <see cref="TableSchema"/>,
/// enabling validation and mapping of column headers to domain-specific column definitions.
/// </summary>
public sealed class CsvHeaderBinder
{
    public sealed record Binding(
        IReadOnlyList<string> FileTokens,
        IReadOnlyDictionary<int, ColumnDefinition> FileIndexToColumn
    );

    public (Binding? Result, string? Error) Bind(string[] headerTokens, TableSchema schema)
    {
        if (headerTokens.Length == 0)
            return (null, "Empty header");

        var byCode = schema.GetColumns().ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

        var map = new Dictionary<int, ColumnDefinition>(headerTokens.Length);
        var tokens = new List<string>(headerTokens.Length);

        for (int i = 0; i < headerTokens.Length; i++)
        {
            var token = headerTokens[i].Trim();
            if (!byCode.TryGetValue(token, out var def))
                return (null, $"Unknown column in header at index {i}: '{token}'");
            map[i] = def;
            tokens.Add(token);
        }

        var expected = schema.GetColumns()
            .Where(c => c.ReadOnly == false)
            .OrderBy(c => c.Index)
            .Select(c => c.Code)
            .ToArray();

        if (!expected.SequenceEqual(tokens, StringComparer.OrdinalIgnoreCase))
            return (null, "Header mismatch: file columns do not match current TableSchema");

        return (new Binding(tokens, map), null);
    }
}