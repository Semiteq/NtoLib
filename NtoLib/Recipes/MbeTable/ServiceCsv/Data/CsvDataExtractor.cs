using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ServiceCsv.Errors;
using NtoLib.Recipes.MbeTable.ServiceCsv.Parsing;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Data;

public sealed class CsvDataExtractor
{
	private readonly IReadOnlyList<ColumnDefinition> _columns;
	private readonly CsvHelperFactory _csvHelperFactory;
	private readonly CsvHeaderBinder _headerBinder;

	public CsvDataExtractor(
		CsvHelperFactory csvHelperFactory,
		CsvHeaderBinder headerBinder,
		IReadOnlyList<ColumnDefinition> columns)
	{
		_csvHelperFactory = csvHelperFactory ?? throw new ArgumentNullException(nameof(csvHelperFactory));
		_headerBinder = headerBinder ?? throw new ArgumentNullException(nameof(headerBinder));
		_columns = columns ?? throw new ArgumentNullException(nameof(columns));
	}

	public Result<CsvRawData> ExtractRawData(TextReader reader)
	{
		using var csvReader = _csvHelperFactory.CreateReader(reader);

		if (!csvReader.Read())
		{
			return new CsvInvalidDataError("Missing header");
		}

		csvReader.ReadHeader();
		var headers = csvReader.HeaderRecord ?? Array.Empty<string>();

		if (headers.Length == 0)
		{
			return new CsvEmptyHeaderError();
		}

		var bindingResult = _headerBinder.Bind(headers, new TableColumns(_columns));
		if (bindingResult.IsFailed)
		{
			return bindingResult.ToResult<CsvRawData>();
		}

		var rows = new List<string>();
		var records = new List<string[]>();

		while (csvReader.Read())
		{
			var record = csvReader.Context.Reader?.Parser.Record ?? Array.Empty<string>();
			var canonicalRow = BuildCanonicalRow(record);

			rows.Add(canonicalRow);
			records.Add(record);
		}

		return Result.Ok(new CsvRawData { Headers = headers, Rows = rows, Records = records });
	}

	private string BuildCanonicalRow(string[] record)
	{
		var stringBuilder = new StringBuilder();
		using var stringWriter = new StringWriter(stringBuilder);
		using var csvWriter = _csvHelperFactory.CreateWriter(stringWriter);

		foreach (var field in record)
		{
			csvWriter.WriteField(field);
		}

		csvWriter.NextRecord();

		return stringBuilder.ToString().TrimEnd('\r', '\n');
	}
}
