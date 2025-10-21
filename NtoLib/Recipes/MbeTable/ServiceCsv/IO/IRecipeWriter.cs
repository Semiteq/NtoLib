using System.IO;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.IO;

/// <summary>
/// Orchestrates the recipe writing pipeline.
/// </summary>
public interface IRecipeWriter
{
    /// <summary>
    /// Writes a recipe to the provided text writer.
    /// </summary>
    /// <param name="recipe">Recipe to write.</param>
    /// <param name="writer">Target text writer.</param>
    /// <returns>Result indicating success or failure.</returns>
    Result WriteAsync(Recipe recipe, TextWriter writer);
}