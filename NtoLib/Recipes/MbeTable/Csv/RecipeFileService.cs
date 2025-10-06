using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Core.Entities;
using NtoLib.Recipes.MbeTable.Csv.Data;
using NtoLib.Recipes.MbeTable.Csv.Integrity;
using NtoLib.Recipes.MbeTable.Csv.IO;
using NtoLib.Recipes.MbeTable.Csv.Metadata;
using NtoLib.Recipes.MbeTable.Journaling.Errors;

namespace NtoLib.Recipes.MbeTable.Csv;

/// <summary>
/// Handles raw file I/O operations for recipe CSV files.
/// </summary>
public sealed class RecipeFileService : IRecipeFileService
{
    private readonly ICsvDataExtractor _dataExtractor;
    private readonly IRecipeWriter _writer;
    private readonly IMetadataService _metadataService;
    private readonly IIntegrityService _integrityService;
    private readonly ILogger _logger;
    private readonly object _fileLock = new();

    public RecipeFileService(
        ICsvDataExtractor dataExtractor,
        IRecipeWriter writer,
        IMetadataService metadataService,
        IIntegrityService integrityService,
        ILogger logger)
    {
        _dataExtractor = dataExtractor ?? throw new ArgumentNullException(nameof(dataExtractor));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
        _integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<CsvRawData>> ReadRawDataAsync(string filePath, Encoding? encoding = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Result.Fail<CsvRawData>(new Error("File path cannot be empty")
                .WithMetadata(nameof(ErrorCode), ErrorCode.IoReadError));
        }

        return await Task.Run(() =>
        {
            lock (_fileLock)
            {
                return ReadRawDataInternal(filePath, encoding);
            }
        });
    }

    public async Task<Result> WriteRecipeAsync(Recipe recipe, string filePath, Encoding? encoding = null)
    {
        if (recipe == null)
        {
            return Result.Fail(new Error("Recipe cannot be null")
                .WithMetadata(nameof(ErrorCode), ErrorCode.BusinessInvariantViolation));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Result.Fail(new Error("File path cannot be empty")
                .WithMetadata(nameof(ErrorCode), ErrorCode.IoWriteError));
        }

        return await Task.Run(() =>
        {
            lock (_fileLock)
            {
                return WriteRecipeInternal(recipe, filePath, encoding);
            }
        });
    }

    private Result<CsvRawData> ReadRawDataInternal(string filePath, Encoding? encoding)
    {
        if (!File.Exists(filePath))
        {
            var error = new Error($"File not found: {filePath}")
                .WithMetadata(nameof(ErrorCode), ErrorCode.IoReadError);
            _logger.LogError($"Read failed - file not found: {filePath}");
            return Result.Fail(error);
        }

        try
        {
            var actualEncoding = encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true, throwOnInvalidBytes: true);
            
            using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream, actualEncoding);
            
            var fullText = reader.ReadToEnd();
            
            var (metadata, metadataLineCount) = _metadataService.ReadMetadata(fullText);
            
            var bodyText = ExtractBodyText(fullText, metadataLineCount);
            
            using var bodyReader = new StringReader(bodyText);
            var extractResult = _dataExtractor.ExtractRawData(bodyReader);
            
            if (extractResult.IsFailed)
            {
                return extractResult;
            }
            
            var rawData = extractResult.Value;
            rawData.Metadata = metadata;
            
            var integrityResult = VerifyIntegrity(metadata, rawData);
            if (integrityResult.IsFailed)
            {
                _logger.LogWarning($"Integrity check failed for: {filePath}");
            }
            
            return Result.Ok(rawData);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to read file: {FilePath}", filePath);
            return Result.Fail<CsvRawData>(new Error($"Failed to read file: {ex.Message}")
                .WithMetadata(nameof(ErrorCode), ErrorCode.IoReadError)
                .CausedBy(ex));
        }
    }

    private Result WriteRecipeInternal(Recipe recipe, string filePath, Encoding? encoding)
    {
        var tempPath = $"{filePath}.tmp";
        
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var actualEncoding = encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, actualEncoding))
            {
                var writeResult = _writer.WriteAsync(recipe, writer);
                if (writeResult.IsFailed)
                {
                    return writeResult;
                }

                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            ReplaceFile(tempPath, filePath);
            
            _logger.LogDebug($"Successfully wrote recipe to: {filePath}");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to write file: {FilePath}", filePath);
            return Result.Fail(new Error($"Failed to write file: {ex.Message}")
                .WithMetadata(nameof(ErrorCode), ErrorCode.IoWriteError)
                .CausedBy(ex));
        }
        finally
        {
            CleanupTempFile(tempPath);
        }
    }

    private string ExtractBodyText(string fullText, int metadataLineCount)
    {
        using var stringReader = new StringReader(fullText);
        
        for (var i = 0; i < metadataLineCount; i++)
        {
            stringReader.ReadLine();
        }
        
        return stringReader.ReadToEnd() ?? string.Empty;
    }

    private Result VerifyIntegrity(RecipeFileMetadata metadata, CsvRawData rawData)
    {
        if (metadata.Rows > 0 && metadata.Rows != rawData.Rows.Count)
        {
            return Result.Fail(new Error("Row count mismatch")
                .WithMetadata(nameof(ErrorCode), ErrorCode.IoReadError)
                .WithMetadata("Expected", metadata.Rows)
                .WithMetadata("Actual", rawData.Rows.Count));
        }
        
        if (!string.IsNullOrWhiteSpace(metadata.BodyHashBase64))
        {
            var actualHash = _integrityService.CalculateHash(rawData.Rows);
            var integrityCheck = _integrityService.VerifyIntegrity(metadata.BodyHashBase64, actualHash);
            
            if (!integrityCheck.IsValid)
            {
                return Result.Fail(new Error("Data integrity check failed")
                    .WithMetadata(nameof(ErrorCode), ErrorCode.IoReadError)
                    .WithMetadata("ExpectedHash", integrityCheck.ExpectedHash)
                    .WithMetadata("ActualHash", integrityCheck.ActualHash));
            }
        }
        
        return Result.Ok();
    }

    private static void ReplaceFile(string sourcePath, string targetPath)
    {
        if (File.Exists(targetPath))
        {
            File.Replace(sourcePath, targetPath, null);
        }
        else
        {
            File.Move(sourcePath, targetPath);
        }
    }

    private void CleanupTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to delete temp file: {TempPath}", tempPath);
        }
    }
}