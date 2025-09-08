#nullable enable

using System.Text;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence;

public interface IRecipeFileReader
{
    /// <summary>
    /// Reads a recipe file from the specified path and deserializes its content.
    /// </summary>
    /// <param name="path">The file system path to the recipe file.</param>
    /// <param name="encoding">The text encoding to use. Defaults to UTF-8.</param>
    /// <returns>A <see cref="Result{T}"/> containing the recipe or an error.</returns>
    Result<Recipe> ReadRecipe(string path, Encoding? encoding = null);
}