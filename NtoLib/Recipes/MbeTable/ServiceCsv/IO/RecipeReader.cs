using System;
using System.IO;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.ServiceCsv.Data;
using NtoLib.Recipes.MbeTable.ServiceCsv.Integrity;
using NtoLib.Recipes.MbeTable.ServiceCsv.Metadata;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.IO;

/// <summary>
/// Extracts raw CSV data from file, verifies metadata and integrity.
/// </summary>
public sealed class RecipeReader : IRecipeReader
{
    private readonly ICsvDataExtractor _dataExtractor;
    private readonly IMetadataService _metadataService;
    private readonly IIntegrityService _integrityService;
    private readonly ILogger<RecipeReader> _logger;

    public RecipeReader(
        ICsvDataExtractor dataExtractor,
        IMetadataService metadataService,
        IIntegrityService integrityService,
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
            return Result.Fail(new Error("Row count mismatch")
                .WithMetadata(nameof(Codes), Codes.IoReadError)
                .WithMetadata("Expected", meta.Rows)
                .WithMetadata("Actual", data.Rows.Count));
        }

        if (!string.IsNullOrWhiteSpace(meta.BodyHashBase64))
        {
            var actual = _integrityService.CalculateHash(data.Rows);
            var check = _integrityService.VerifyIntegrity(meta.BodyHashBase64, actual);
            if (!check.IsValid)
            {
                return Result.Fail(new Error("Body hash mismatch")
                    .WithMetadata(nameof(Codes), Codes.IoReadError)
                    .WithMetadata("ExpectedHash", check.ExpectedHash)
                    .WithMetadata("ActualHash", check.ActualHash));
            }
        }

        return Result.Ok();
    }
}