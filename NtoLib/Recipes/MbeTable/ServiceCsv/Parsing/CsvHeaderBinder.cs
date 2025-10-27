using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Parsing;

/// <summary>
/// A utility class to bind CSV file headers to a specified <see cref="TableColumns"/>,
/// enabling validation and mapping of column headers to domain-specific column definitions.
/// </summary>
public sealed class CsvHeaderBinder : ICsvHeaderBinder
{
    public sealed record Binding(
        IReadOnlyList<string> FileTokens,
        IReadOnlyDictionary<short, ColumnDefinition> FileIndexToColumn
    );

    public Result<Binding> Bind(string[] headerTokens, TableColumns columns)
    {
        if (headerTokens.Length == 0)
            return Result.Fail(new Error("Empty header").WithMetadata(nameof(Codes), Codes.CsvHeaderMismatch));

        var byCode = columns.GetColumns().ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

        var map = new Dictionary<short, ColumnDefinition>(headerTokens.Length);
        var tokens = new List<string>(headerTokens.Length);

        for (short i = 0; i < headerTokens.Length; i++)
        {
            var token = headerTokens[i].Trim();
            if (!byCode.TryGetValue(token, out var def))
                return Result.Fail(
                    new Error($"Unknown column in header at index {i}: '{token}'").WithMetadata(nameof(Codes),
                        Codes.CsvHeaderMismatch));

            map[i] = def;
            tokens.Add(token);
        }

        var expected = columns.GetColumns()
            .Where(c => !c.ReadOnly)
            .Select(c => c.Code)
            .ToArray();

        if (!expected.SequenceEqual(tokens, StringComparer.OrdinalIgnoreCase))
            return Result.Fail(
                new Error("Header mismatch: file columns do not match current TableSchema").WithMetadata(nameof(Codes),
                    Codes.CsvHeaderMismatch));

        return Result.Ok(new Binding(tokens, map));
    }
}