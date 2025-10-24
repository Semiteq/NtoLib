using System.Text;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ServiceCsv.Data;

namespace NtoLib.Recipes.MbeTable.ServiceCsv;

/// <summary>
/// Handles raw file I/O operations for recipe CSV files.
/// </summary>
public interface IRecipeFileService
{
    /// <summary>
    /// Reads raw CSV data from the specified file path.
    /// </summary>
    /// <param name="filePath">Path to the CSV file.</param>
    /// <param name="encoding">Text encoding. Defaults to UTF-8 with BOM.</param>
    /// <returns>Result containing raw CSV data or error information.</returns>
    Task<Result<CsvRawData>> ReadRawDataAndCheckIntegrityAsync(string filePath, Encoding? encoding = null);

    /// <summary>
    /// Writes a recipe to the specified file path.
    /// </summary>
    /// <param name="recipe">Recipe to write.</param>
    /// <param name="filePath">Target file path.</param>
    /// <param name="encoding">Text encoding. Defaults to UTF-8 with BOM.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> WriteRecipeAsync(Recipe recipe, string filePath, Encoding? encoding = null);
}