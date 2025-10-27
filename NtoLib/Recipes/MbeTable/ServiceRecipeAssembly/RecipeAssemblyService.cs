using System;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ServiceCsv.Data;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Strategies;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Validation;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly;

public sealed class RecipeAssemblyService : IRecipeAssemblyService
{
    private readonly ModbusAssemblyStrategy _modbusStrategy;
    private readonly CsvAssemblyStrategy _csvStrategy;
    private readonly AssemblyValidator _validator;
    private readonly ILogger<RecipeAssemblyService> _logger;

    public RecipeAssemblyService(
        ModbusAssemblyStrategy modbusStrategy,
        CsvAssemblyStrategy csvStrategy,
        AssemblyValidator validator,
        ILogger<RecipeAssemblyService> logger)
    {
        _modbusStrategy = modbusStrategy ?? throw new ArgumentNullException(nameof(modbusStrategy));
        _csvStrategy = csvStrategy ?? throw new ArgumentNullException(nameof(csvStrategy));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Result<Recipe> AssembleFromModbusData(int[] intData, int[] floatData, int rowCount)
    {
        _logger.LogDebug("Starting Modbus assembly for {RowCount} rows", rowCount);
        
        var assemblyResult = _modbusStrategy.AssembleFromModbusData(intData, floatData, rowCount);
        if (assemblyResult.IsFailed)
        {
            _logger.LogError("Modbus assembly failed");
            return assemblyResult;
        }
        
        var recipe = assemblyResult.Value;
        _logger.LogDebug("Assembled {StepsCount} steps from Modbus", recipe.Steps.Count);
        
        var validationResult = _validator.ValidateRecipe(recipe);
        if (validationResult.IsFailed)
        {
            _logger.LogError("Recipe validation failed after Modbus assembly");
            return validationResult.ToResult<Recipe>();
        }
        
        var result = Result.Ok(recipe);
        if (validationResult.Reasons.Count > 0)
            result = result.WithReasons(validationResult.Reasons);
        
        _logger.LogDebug("Modbus assembly completed successfully");
        return result;
    }

    public Result<Recipe> AssembleFromCsvData(object csvData)
    {
        if (csvData is not CsvRawData rawData)
        {
            return Result.Fail<Recipe>("Invalid CSV data type");
        }
        
        _logger.LogDebug("Starting CSV assembly for {RecordCount} rows", rawData.Records.Count);
        
        var assemblyResult = _csvStrategy.AssembleFromRawData(rawData);
        if (assemblyResult.IsFailed)
        {
            _logger.LogError("CSV assembly failed");
            return assemblyResult;
        }
        
        var recipe = assemblyResult.Value;
        _logger.LogDebug("Assembled {StepsCount} steps from CSV", recipe.Steps.Count);
        
        var validationResult = _validator.ValidateRecipe(recipe);
        if (validationResult.IsFailed)
        {
            _logger.LogError("Recipe validation failed after CSV assembly");
            return validationResult.ToResult<Recipe>();
        }
        
        var result = Result.Ok(recipe);
        if (validationResult.Reasons.Count > 0)
            result = result.WithReasons(validationResult.Reasons);
        
        _logger.LogDebug("CSV assembly completed successfully");
        return result;
    }
}