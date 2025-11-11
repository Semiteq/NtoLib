using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ServiceCsv.Data;
using NtoLib.Recipes.MbeTable.ServiceCsv.Errors;
using NtoLib.Recipes.MbeTable.ServiceCsv.Integrity;
using NtoLib.Recipes.MbeTable.ServiceCsv.IO;
using NtoLib.Recipes.MbeTable.ServiceCsv.Metadata;
using NtoLib.Recipes.MbeTable.ServiceCsv.Warnings;

namespace NtoLib.Recipes.MbeTable.ServiceCsv;

public sealed class RecipeFileService : IRecipeFileService
{
    private readonly ICsvDataExtractor _dataExtractor;
    private readonly IRecipeWriter _writer;
    private readonly IMetadataService _metadataService;
    private readonly IIntegrityService _integrityService;
    private readonly ILogger<RecipeFileService> _logger;
    private readonly object _fileLock = new();

    public RecipeFileService(
        ICsvDataExtractor dataExtractor,
        IRecipeWriter writer,
        IMetadataService metadataService,
        IIntegrityService integrityService,
        ILogger<RecipeFileService> logger)
    {
        _dataExtractor = dataExtractor ?? throw new ArgumentNullException(nameof(dataExtractor));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
        _integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<CsvRawData>> ReadRawDataAndCheckIntegrityAsync(string filePath, Encoding? encoding = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return new CsvFilePathEmptyError();
        

        return await Task.Run(() =>
        {
            lock (_fileLock)
            {
                return ReadRawDataAndCheckIntegrityInternal(filePath, encoding);
            }
        });
    }

    public async Task<Result> WriteRecipeAsync(Recipe recipe, string filePath, Encoding? encoding = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return new CsvFilePathEmptyError();
        }

        return await Task.Run(() =>
        {
            lock (_fileLock)
            {
                return WriteRecipeInternal(recipe, filePath, encoding);
            }
        });
    }

    private Result<CsvRawData> ReadRawDataAndCheckIntegrityInternal(string filePath, Encoding? encoding)
    {
        if (!File.Exists(filePath))
        {
            var error = new CsvFileNotFoundError(filePath);
            _logger.LogError("Read failed - file not found: {FilePath}", filePath);
            return Result.Fail(error);
        }

        try
        {
            var actualEncoding = encoding ??
                                 new UTF8Encoding(encoderShouldEmitUTF8Identifier: true, throwOnInvalidBytes: true);

            using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream, actualEncoding);

            var fullText = reader.ReadToEnd();

            var (metadata, metadataLineCount) = _metadataService.ReadMetadata(fullText);
            var bodyText = ExtractBodyText(fullText, metadataLineCount);

            using var bodyReader = new StringReader(bodyText);

            var extractedRawData = _dataExtractor.ExtractRawData(bodyReader);
            if (extractedRawData.IsFailed) 
                return extractedRawData;
            
            var integrityResult = VerifyIntegrity(metadata, extractedRawData.Value);
            
            return Result.Ok(extractedRawData.Value).WithReasons(integrityResult.Reasons);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to read file: {FilePath}", filePath);
            return new CsvReadFailedError(ex.Message).CausedBy(ex);
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

            _logger.LogDebug("Successfully wrote recipe to: {FilePath}", filePath);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to write file: {FilePath}", filePath);
            return new CsvWriteFailedError(ex.Message).CausedBy(ex);
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
        var result = Result.Ok();

        if (metadata.Rows > 0 && metadata.Rows != rawData.Rows.Count)
        {
            _logger.LogWarning("Row count mismatch. Expected={Expected}, Actual={Actual}", metadata.Rows, rawData.Rows.Count);
            result = result.WithReason(new CsvRowCountMismatchWarning(metadata.Rows, rawData.Rows.Count));
        }

        if (!string.IsNullOrWhiteSpace(metadata.BodyHashBase64))
        {
            var actualHash = _integrityService.CalculateHash(rawData.Rows);
            var integrityCheck = _integrityService.VerifyIntegrity(metadata.BodyHashBase64, actualHash);

            if (!integrityCheck.IsValid)
            {
                _logger.LogWarning("Hash mismatch. Expected={Expected}, Actual={Actual}", integrityCheck.ExpectedHash, integrityCheck.ActualHash);
                result = result.WithReason(new CsvHashMismatchWarning(integrityCheck.ExpectedHash, integrityCheck.ActualHash));
            }
        }

        return result;
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