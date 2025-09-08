#nullable enable

using System.Text;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence;

public interface IRecipeFileWriter
{
    /// <summary>
    /// Writes a recipe to the specified path.
    /// </summary>
    /// <param name="recipe">The recipe to write.</param>
    /// <param name="path">The file system path to write to.</param>
    /// <param name="encoding">The text encoding to use. Defaults to UTF-8 with BOM.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Result WriteRecipe(Recipe recipe, string path, Encoding? encoding = null);
}