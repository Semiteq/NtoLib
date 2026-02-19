using System;
using System.IO;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ServiceCsv.Data;
using NtoLib.Recipes.MbeTable.ServiceCsv.Integrity;
using NtoLib.Recipes.MbeTable.ServiceCsv.Metadata;
using NtoLib.Recipes.MbeTable.ServiceCsv.Warnings;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.IO;

public sealed class RecipeReader
{
	private readonly CsvDataExtractor _dataExtractor;
	private readonly MetadataService _metadataService;
	private readonly IntegrityService _integrityService;
	private readonly ILogger<RecipeReader> _logger;

	public RecipeReader(
		CsvDataExtractor dataExtractor,
		MetadataService metadataService,
		IntegrityService integrityService,
		ILogger<RecipeReader> logger)
	{
		_dataExtractor = dataExtractor ?? throw new ArgumentNullException(nameof(dataExtractor));
		_metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
		_integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public Result<CsvRawData> ReadAsync(TextReader reader)
	{
		_logger.LogDebug("Starting CSV read (raw mode)");

		var fullText = reader.ReadToEnd();
		var (metadata, metaLines) = _metadataService.ReadMetadata(fullText);

		var bodyText = ExtractBody(fullText, metaLines);
		using var bodyReader = new StringReader(bodyText);

		var extractResult = _dataExtractor.ExtractRawData(bodyReader);
		if (extractResult.IsFailed)
			return extractResult;

		var raw = extractResult.Value;
		raw.Metadata = metadata;

		var integrity = VerifyIntegrity(metadata, raw);
		return integrity.IsFailed ? integrity.ToResult<CsvRawData>() : Result.Ok(raw);
	}

	private static string ExtractBody(string fullText, int metaLines)
	{
		using var sr = new StringReader(fullText);
		for (var i = 0; i < metaLines; i++)
			sr.ReadLine();
		return sr.ReadToEnd() ?? string.Empty;
	}

	private Result VerifyIntegrity(RecipeFileMetadata meta, CsvRawData data)
	{
		if (meta.Rows > 0 && meta.Rows != data.Rows.Count)
		{
			_logger.LogWarning("CSV row count mismatch: expected {Expected}, actual {Actual}", meta.Rows,
				data.Rows.Count);
			return Result.Ok().WithReason(new CsvRowCountMismatchWarning(meta.Rows, data.Rows.Count));
		}

		if (!string.IsNullOrWhiteSpace(meta.BodyHashBase64))
		{
			var actual = IntegrityService.CalculateHash(data.Rows);
			var check = IntegrityService.VerifyIntegrity(meta.BodyHashBase64, actual);
			if (!check.IsValid)
			{
				_logger.LogWarning("CSV body hash mismatch: expected {ExpectedHash}, actual {ActualHash}",
					check.ExpectedHash, check.ActualHash);
				return Result.Ok().WithReason(new CsvHashMismatchWarning());
			}
		}

		return Result.Ok();
	}
}
