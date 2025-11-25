using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Csv;

/// <summary>
/// Executes high-level CSV recipe operations (read / write).
/// Owns full pipeline: disk I/O → extraction / formatting → assembly → validation.
/// </summary>
public interface ICsvService
{
	/// <summary>
	/// Reads a recipe from CSV file with full assembly and validation pipeline.
	/// </summary>
	/// <param name="filePath">Path to the CSV file.</param>
	/// <returns>Result containing assembled and validated recipe.</returns>
	Task<Result<Recipe>> ReadCsvAsync(string filePath);

	/// <summary>
	/// Writes a recipe to CSV file.
	/// </summary>
	/// <param name="recipe">Recipe to write.</param>
	/// <param name="filePath">Target file path.</param>
	/// <returns>Result indicating success or failure.</returns>
	Task<Result> WriteCsvAsync(Recipe recipe, string filePath);
}
