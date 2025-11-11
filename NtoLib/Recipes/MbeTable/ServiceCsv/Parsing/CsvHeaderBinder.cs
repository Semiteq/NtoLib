using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ServiceCsv.Errors;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Parsing;

public sealed class CsvHeaderBinder : ICsvHeaderBinder
{
    public sealed record Binding(
        IReadOnlyList<string> FileTokens,
        IReadOnlyDictionary<short, ColumnDefinition> FileIndexToColumn
    );

    public Result<Binding> Bind(string[] headerTokens, TableColumns columns)
    {
        var validationResult = ValidateHeaderTokens(headerTokens);
        if (validationResult.IsFailed)
            return validationResult.ToResult<Binding>();

        var byCode = BuildColumnLookup(columns);
        var expected = GetExpectedColumnCodes(columns);

        var mapResult = MapHeaderTokensToColumns(headerTokens, byCode, expected);
        if (mapResult.IsFailed)
            return mapResult.ToResult<Binding>();

        var (map, tokens) = mapResult.Value;

        var sequenceResult = ValidateHeaderSequence(expected, tokens);
        if (sequenceResult.IsFailed)
            return sequenceResult.ToResult<Binding>();

        return new Binding(tokens, map);
    }

    private static Result ValidateHeaderTokens(string[] headerTokens)
    {
        return headerTokens.Length == 0 
            ? new CsvEmptyHeaderError() 
            : Result.Ok();
    }

    private static Dictionary<string, ColumnDefinition> BuildColumnLookup(TableColumns columns)
    {
        return columns.GetColumns()
            .ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);
    }

    private static string[] GetExpectedColumnCodes(TableColumns columns)
    {
        return columns.GetColumns()
            .Where(c => c.SaveToCsv)
            .Select(c => c.Code)
            .ToArray();
    }

    private static Result<(Dictionary<short, ColumnDefinition> Map, List<string> Tokens)> MapHeaderTokensToColumns(
        string[] headerTokens,
        Dictionary<string, ColumnDefinition> byCode,
        string[] expected)
    {
        var map = new Dictionary<short, ColumnDefinition>(headerTokens.Length);
        var tokens = new List<string>(headerTokens.Length);

        for (short i = 0; i < headerTokens.Length; i++)
        {
            var token = headerTokens[i].Trim();

            if (!byCode.TryGetValue(token, out var def))
                return new CsvHeaderMismatchError(expected, headerTokens);

            map[i] = def;
            tokens.Add(token);
        }

        return (map, tokens);
    }

    private static Result ValidateHeaderSequence(string[] expected, List<string> actual)
    {
        return expected.SequenceEqual(actual, StringComparer.OrdinalIgnoreCase) 
            ? Result.Ok()
            : new CsvHeaderMismatchError(expected, actual.ToArray());
    }
}