

using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Entities;

namespace NtoLib.Recipes.MbeTable.RecipeAssemblyService;

/// <summary>
/// Service for assembling Recipe from raw data sources (PLC, CSV, etc.).
/// </summary>
public interface IRecipeAssemblyService
{
    /// <summary>
    /// Assembles a Recipe from raw Modbus data arrays.
    /// </summary>
    Result<Recipe> AssembleFromModbusData(int[] intData, int[] floatData, int rowCount);
    
    /// <summary>
    /// Assembles a Recipe from CSV raw data.
    /// </summary>
    Result<Recipe> AssembleFromCsvData(object csvData);
}