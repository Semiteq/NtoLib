using System;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ServiceCsv;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Validation;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations;

/// <summary>
/// Executes high-level CSV recipe operations (read / write).
/// Owns full pipeline: disk I/O → extraction / formatting → assembly → validation.
/// </summary>
public sealed class CsvService : ICsvService
{
    private readonly IRecipeFileService _fileService;
    private readonly IRecipeAssemblyService _assemblyService;
    private readonly AssemblyValidator _validator;
    private readonly ILogger<CsvService> _logger;

    public CsvService(
        IRecipeFileService fileService,
        IRecipeAssemblyService assemblyService,
        AssemblyValidator validator,
        ILogger<CsvService> logger)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _assemblyService = assemblyService ?? throw new ArgumentNullException(nameof(assemblyService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Recipe>> ReadCsvAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Result.Fail(
                new Error("File path cannot be empty").WithMetadata(nameof(Codes), Codes.IoFileNotFound));

        try
        {
            var rawDataResult = await _fileService.ReadRawDataAndCheckIntegrityAsync(filePath);
            if (rawDataResult.IsFailed) return rawDataResult.ToResult();

            var rawData = rawDataResult.Value;
            _logger.LogDebug("Read {RecordsCount} rows from CSV", rawData.Records.Count);

            var assemblyResult = _assemblyService.AssembleFromCsvData(rawData);
            if (assemblyResult.IsFailed) return assemblyResult;

            var recipe = assemblyResult.Value;
            _logger.LogDebug("Assembled recipe with {StepsCount} steps", recipe.Steps.Count);

            var validationResult = _validator.ValidateRecipe(recipe);
            if (validationResult.IsFailed) return validationResult;

            // rawDataResult may contain a reason for hash mismatch
            return Result.Ok(recipe).WithReasons(rawDataResult.Reasons);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error reading CSV from {FilePath}", filePath);
            return Result.Fail<Recipe>(
                new Error($"Unexpected error reading CSV: {ex.Message}").WithMetadata(nameof(Codes),
                    Codes.IoReadError));
        }
    }

    public async Task<Result> WriteCsvAsync(Recipe recipe, string filePath)
    {
        if (recipe == null)
            return Result.Fail(
                new Error("Recipe cannot be null").WithMetadata(nameof(Codes), Codes.CoreInvalidOperation));

        if (string.IsNullOrWhiteSpace(filePath))
            return Result.Fail(
                new Error("File path cannot be empty").WithMetadata(nameof(Codes), Codes.IoFileNotFound));

        try
        {
            var validationResult = _validator.ValidateRecipe(recipe);
            if (validationResult.IsFailed) return validationResult;

            var writeResult = await _fileService.WriteRecipeAsync(recipe, filePath);
            if (writeResult.IsFailed) return writeResult;

            writeResult.WithReasons(validationResult.Reasons);
            return writeResult;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error writing CSV to {FilePath}. Step count: {StepCount}", filePath,
                recipe.Steps.Count);
            return Result.Fail(new Error($"Unexpected error writing CSV: {ex.Message}").WithMetadata(nameof(Codes),
                Codes.IoReadError));
        }
    }
}