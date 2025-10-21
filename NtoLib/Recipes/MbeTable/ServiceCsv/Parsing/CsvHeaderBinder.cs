using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Parsing;

/// <summary>
/// A utility class to bind CSV file headers to a specified <see cref="TableColumns"/>,
/// enabling validation and mapping of column headers to domain-specific column definitions.
/// </summary>
public sealed class CsvHeaderBinder : ICsvHeaderBinder
{
    public sealed record Binding(
        IReadOnlyList<string> FileTokens,
        IReadOnlyDictionary<int, ColumnDefinition> FileIndexToColumn
    );

    public Result<Binding> Bind(string[] headerTokens, TableColumns columns)
    {
        if (headerTokens.Length == 0)
            return Result.Fail("Empty header");

        var byCode = columns.GetColumns().ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

        var map = new Dictionary<int, ColumnDefinition>(headerTokens.Length);
        var tokens = new List<string>(headerTokens.Length);

        for (var i = 0; i < headerTokens.Length; i++)
        {
            var token = headerTokens[i].Trim();
            if (!byCode.TryGetValue(token, out var def))
                return Result.Fail($"Unknown column in header at index {i}: '{token}'");
            
            map[i] = def;
            tokens.Add(token);
        }

        var expected = columns.GetColumns()
            .Where(c => !c.ReadOnly)
            .Select(c => c.Code)
            .ToArray();

        if (!expected.SequenceEqual(tokens, StringComparer.OrdinalIgnoreCase))
            return Result.Fail("Header mismatch: file columns do not match current TableSchema");

        return Result.Ok(new Binding(tokens, map));
    }
}