using System;
using System.Threading.Tasks;
using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Core.Entities;
using NtoLib.Recipes.MbeTable.Csv;
using NtoLib.Recipes.MbeTable.RecipeAssemblyService;
using NtoLib.Recipes.MbeTable.RecipeAssemblyService.Validation;

namespace NtoLib.Recipes.MbeTable.Application.Operations;

public sealed class CsvService : ICsvService
{
    private readonly IRecipeFileService _fileService;
    private readonly IRecipeAssemblyService _assemblyService;
    private readonly AssemblyValidator _validator;
    private readonly ILogger _logger;

    public CsvService(
        IRecipeFileService fileService,
        IRecipeAssemblyService assemblyService,
        AssemblyValidator validator,
        ILogger logger)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _assemblyService = assemblyService ?? throw new ArgumentNullException(nameof(assemblyService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Recipe>> ReadCsvAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogError("File path is empty");
            return Result.Fail<Recipe>("File path cannot be empty");
        }

        _logger.LogError($"Starting CSV read operation from: {filePath}");

        try
        {
            var rawDataResult = await _fileService.ReadRawDataAsync(filePath);
            if (rawDataResult.IsFailed)
            {
                _logger.LogError($"Failed to read raw CSV data from: {filePath}: {string.Join(", ", rawDataResult.Errors)}");
                return rawDataResult.ToResult<Recipe>();
            }

            var rawData = rawDataResult.Value;
            _logger.LogDebug($"Read {rawData.Records.Count} rows from CSV");

            var assemblyResult = _assemblyService.AssembleFromCsvData(rawData);
            if (assemblyResult.IsFailed)
            {
                _logger.LogError($"Failed to assemble recipe from CSV data: {string.Join(", ", assemblyResult.Errors)}");
                return assemblyResult;
            }

            var recipe = assemblyResult.Value;
            _logger.LogDebug($"Assembled recipe with {recipe.Steps.Count} steps");

            var validationResult = _validator.ValidateRecipe(recipe);
            if (validationResult.IsFailed)
            {
                _logger.LogWarning($"Recipe validation failed: {string.Join(", ", validationResult.Errors)}");
                return Result.Ok(recipe).WithErrors(validationResult.Errors);
            }

            _logger.LogDebug("CSV read operation completed successfully");
            return Result.Ok(recipe);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error reading CSV from {FilePath}", filePath);
            return Result.Fail<Recipe>($"Unexpected error reading CSV: {ex.Message}");
        }
    }

    public async Task<Result> WriteCsvAsync(Recipe recipe, string filePath)
    {
        if (recipe == null)
        {
            _logger.LogError("Recipe is null");
            return Result.Fail("Recipe cannot be null");
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogError("File path is empty");
            return Result.Fail("File path cannot be empty");
        }

        _logger.LogDebug($"Starting CSV write operation to: {filePath}", LogLevel.Debug);

        try
        {
            var validationResult = _validator.ValidateRecipe(recipe);
            if (validationResult.IsFailed)
            {
                _logger.LogWarning($"Recipe validation failed before write to: {filePath}: {string.Join(", ", validationResult.Errors)}");
            }

            var writeResult = await _fileService.WriteRecipeAsync(recipe, filePath);
            if (writeResult.IsFailed)
            {
                _logger.LogError($"Failed to write recipe to: {filePath}: {string.Join(", ", writeResult.Errors)}");
                return writeResult;
            }

            _logger.LogDebug($"CSV write operation completed successfully to: {filePath}");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error writing CSV to {FilePath}. Step count: {StepCount}", filePath, recipe.Steps.Count);
            return Result.Fail($"Unexpected error writing CSV: {ex.Message}");
        }
    }
}