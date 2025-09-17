#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv;

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

    /// <summary>
    /// Binds the header tokens from a CSV file to the provided table schema.
    /// </summary>
    /// <param name="headerTokens">An array of strings representing the header columns from the CSV file.</param>
    /// <param name="columns">The table schema to bind against.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the binding information on success, or an error on failure.
    /// </returns>
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