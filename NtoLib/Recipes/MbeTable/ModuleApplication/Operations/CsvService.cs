using System;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.Errors;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Recipes.MbeTable.ServiceCsv;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Validation;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations;

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
            return new ApplicationFilePathEmptyError();

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
            if (validationResult.IsFailed) return validationResult.ToResult<Recipe>();

            var result = Result.Ok(recipe);
            
            if (rawDataResult.Reasons.Count > 0)
                result = result.WithReasons(rawDataResult.Reasons);
            
            if (validationResult.Reasons.Count > 0)
                result = result.WithReasons(validationResult.Reasons);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error reading CSV from {FilePath}", filePath);
            return Result.Fail<Recipe>(new BilingualError(
                "Unexpected error reading CSV file",
                "Непредвиденная ошибка при чтении CSV файла").CausedBy(ex));
        }
    }

    public async Task<Result> WriteCsvAsync(Recipe recipe, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return new ApplicationFilePathEmptyError();

        try
        {
            var validationResult = _validator.ValidateRecipe(recipe);
            if (validationResult.IsFailed) return validationResult;

            var writeResult = await _fileService.WriteRecipeAsync(recipe, filePath);
            if (writeResult.IsFailed) return writeResult;

            if (validationResult.Reasons.Count > 0)
                writeResult = writeResult.WithReasons(validationResult.Reasons);

            return writeResult;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error writing CSV to {FilePath}. Step count: {StepCount}", filePath,
                recipe.Steps.Count);
            return Result.Fail(new BilingualError(
                "Unexpected error writing CSV file",
                "Непредвиденная ошибка при записи CSV файла").CausedBy(ex));
        }
    }
}