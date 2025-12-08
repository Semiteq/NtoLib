using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using CsvHelper;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ServiceCsv.Parsing;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Data;

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

		var writableColumns = GetColumnsToWriteToCsv(_columns);

		WriteHeader(csvWriter, writableColumns);
		WriteSteps(csvWriter, recipe, writableColumns);

		return stringBuilder.ToString();
	}

	private static ColumnDefinition[] GetColumnsToWriteToCsv(IReadOnlyList<ColumnDefinition> columns)
	{
		return columns.Where(column => column.SaveToCsv).ToArray();
	}

	private static void WriteHeader(CsvWriter csvWriter, IEnumerable<ColumnDefinition> columns)
	{
		foreach (var column in columns)
		{
			csvWriter.WriteField(column.Code);
		}

		csvWriter.NextRecord();
	}

	private static void WriteSteps(CsvWriter csvWriter, Recipe recipe, IReadOnlyList<ColumnDefinition> columns)
	{
		foreach (var step in recipe.Steps)
		{
			WriteStep(csvWriter, step, columns);
		}
	}

	private static void WriteStep(CsvWriter csvWriter, Step step, IReadOnlyList<ColumnDefinition> columns)
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

		var value = property.GetValueAsObject;

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
