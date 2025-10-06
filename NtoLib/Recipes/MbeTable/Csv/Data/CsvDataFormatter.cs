

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Core.Entities;
using NtoLib.Recipes.MbeTable.Csv.Parsing;

namespace NtoLib.Recipes.MbeTable.Csv.Data;

/// <summary>
/// Formats Recipe objects to CSV string representation.
/// </summary>
public sealed class CsvDataFormatter : ICsvDataFormatter
{
    private readonly ICsvHelperFactory _csvHelperFactory;
    private readonly IReadOnlyList<ColumnDefinition> _columns;

    public CsvDataFormatter(
        ICsvHelperFactory csvHelperFactory,
        IReadOnlyList<ColumnDefinition> columns)
    {
        _csvHelperFactory = csvHelperFactory ?? throw new ArgumentNullException(nameof(csvHelperFactory));
        _columns = columns ?? throw new ArgumentNullException(nameof(columns));
    }

    public Result<string> FormatToCsv(Recipe recipe)
    {
        var stringBuilder = new StringBuilder();
        using var stringWriter = new StringWriter(stringBuilder);
        using var csvWriter = _csvHelperFactory.CreateWriter(stringWriter);
        
        var writableColumns = _columns //todo: writing readonly is dangerous default
            .Where(column => !column.ReadOnly)
            .ToArray();
        
        WriteHeader(csvWriter, writableColumns);
        WriteSteps(csvWriter, recipe, writableColumns);
        
        return Result.Ok(stringBuilder.ToString());
    }

    private static void WriteHeader(CsvHelper.CsvWriter csvWriter, IEnumerable<ColumnDefinition> columns)
    {
        foreach (var column in columns)
        {
            csvWriter.WriteField(column.Code);
        }
        
        csvWriter.NextRecord();
    }

    private static void WriteSteps(CsvHelper.CsvWriter csvWriter, Recipe recipe, IReadOnlyList<ColumnDefinition> columns)
    {
        foreach (var step in recipe.Steps)
        {
            WriteStep(csvWriter, step, columns);
        }
    }

    private static void WriteStep(CsvHelper.CsvWriter csvWriter, Step step, IReadOnlyList<ColumnDefinition> columns)
    {
        foreach (var column in columns)
        {
            var value = GetStepValue(step, column);
            csvWriter.WriteField(value);
        }
        
        csvWriter.NextRecord();
    }

    private static string GetStepValue(Step step, ColumnDefinition column)
    {
        if (column.Key == MandatoryColumns.StepStartTime)
        {
            return string.Empty;
        }
        
        if (!step.Properties.TryGetValue(column.Key, out var property) || property == null)
        {
            return string.Empty;
        }
        
        var value = property.GetValueAsObject();
        
        return value switch
        {
            null => string.Empty,
            short shortValue => shortValue.ToString(CultureInfo.InvariantCulture),
            float floatValue => floatValue.ToString("R", CultureInfo.InvariantCulture),
            string stringValue => stringValue,
            _ => value.ToString() ?? string.Empty
        };
    }
}