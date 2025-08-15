#nullable enable

using System.Collections.Immutable;
using System.IO;
using System.Text;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence;

/// <summary>
/// Provides functionality to read recipe files from the filesystem and deserialize them into domain entities.
/// </summary>
public class RecipeFileReader : IRecipeFileReader
{
    private readonly IRecipeSerializer _serializer;
    public RecipeFileReader(IRecipeSerializer serializer) => _serializer = serializer;

    /// <summary>
    /// Reads a recipe file from the specified path, deserializes its content, and returns a tuple containing the recipe
    /// and a list of any errors encountered during the operation.
    /// </summary>
    /// <param name="path">The file system path to the recipe file to be read.</param>
    /// <param name="encoding">The text encoding to be used when reading the file. If null, UTF-8 encoding will be applied by default.</param>
    /// <returns>
    /// A tuple containing the following:
    /// - Recipe: The deserialized <see cref="Recipe"/> object, or null if an error occurred.
    /// - Errors: An immutable list of <see cref="RecipeFileError"/> objects indicating any issues encountered during reading or deserialization.
    /// </returns>
    public (Recipe? Recipe, IImmutableList<RecipeFileError> Errors) Read(string path, Encoding? encoding = null)
    {
        if (!File.Exists(path))
        {
            return (null, ImmutableList<RecipeFileError>.Empty.Add(
                new RecipeFileError(0, null, $"File not found: {path}")));
        }

        using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream, encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true, throwOnInvalidBytes: true));
        return _serializer.Deserialize(reader);
    }
}