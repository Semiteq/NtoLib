using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Csv;

/// <summary>
/// Service for assembling Recipe from CSV raw data.
/// </summary>
public interface ICsvRecipeAssemblyService
{
    /// <summary>
    /// Assembles a Recipe from CSV raw data.
    /// </summary>
    Result<Recipe> AssembleFromCsvData(object csvData);
}