#nullable enable

using System.IO;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence;

public interface IRecipeSerializer
{
    /// <summary>
    /// Deserializes recipe data from the given text reader.
    /// </summary>
    /// <param name="reader">The text reader to read from.</param>
    /// <returns>A <see cref="Result{T}"/> containing the deserialized recipe or an error.</returns>
    Result<Recipe> Deserialize(TextReader reader);
    
    /// <summary>
    /// Serializes the given recipe to the text writer.
    /// </summary>
    /// <param name="recipe">The recipe to serialize.</param>
    /// <param name="writer">The text writer to write to.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Result Serialize(Recipe recipe, TextWriter writer);
}