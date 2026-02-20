using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ServiceCsv.Data;
using NtoLib.Recipes.MbeTable.ServiceCsv.Integrity;
using NtoLib.Recipes.MbeTable.ServiceCsv.Metadata;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.IO;

public sealed class RecipeWriter
{
	private readonly CsvDataFormatter _dataFormatter;
	private readonly IntegrityService _integrityService;
	private readonly ILogger<RecipeWriter> _logger;
	private readonly MetadataService _metadataService;

	public RecipeWriter(
		CsvDataFormatter dataFormatter,
		MetadataService metadataService,
		IntegrityService integrityService,
		ILogger<RecipeWriter> logger)
	{
		_dataFormatter = dataFormatter ?? throw new ArgumentNullException(nameof(dataFormatter));
		_metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
		_integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public Result WriteAsync(Recipe recipe, TextWriter writer)
	{
		_logger.LogDebug($"Starting CSV recipe write with {recipe.Steps.Count} steps");

		var formatResult = _dataFormatter.FormatToCsv(recipe);
		if (formatResult.IsFailed)
		{
			_logger.LogError("Failed to format recipe to CSV");

			return formatResult.ToResult();
		}

		var csvContent = formatResult.Value;
		var dataRows = ExtractDataRows(csvContent);

		_logger.LogDebug($"Formatted {dataRows.Count} data rows");

		var bodyHash = IntegrityService.CalculateHash(dataRows);

		var metadata = new RecipeFileMetadata
		{
			Separator = ';',
			Rows = dataRows.Count,
			BodyHashBase64 = bodyHash,
			Extras = new Dictionary<string, string> { ["ExportedAtLocalTime"] = DateTime.Now.ToString("O") }
		};

		_metadataService.WriteMetadata(writer, metadata);
		writer.Write(csvContent);

		_logger.LogDebug("Recipe write completed successfully");

		return Result.Ok();
	}

	private static List<string> ExtractDataRows(string csvContent)
	{
		var lines = csvContent.Split(new[] { "\r\n" }, StringSplitOptions.None);

		if (lines.Length <= 1)
		{
			return new List<string>();
		}

		return lines.Skip(1).Where(line => !string.IsNullOrEmpty(line)).ToList();
	}
}
