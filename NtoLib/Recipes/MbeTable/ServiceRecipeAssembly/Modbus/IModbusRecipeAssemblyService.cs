using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Modbus;

/// <summary>
/// Service for assembling Recipe from Modbus raw data.
/// </summary>
public interface IModbusRecipeAssemblyService
{
    /// <summary>
    /// Assembles a Recipe from raw Modbus data arrays.
    /// </summary>
    Result<Recipe> AssembleFromModbusData(int[] intData, int[] floatData, int rowCount);
}