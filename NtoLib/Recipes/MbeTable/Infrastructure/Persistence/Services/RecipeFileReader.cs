#nullable enable

using System;
using System.IO;
using System.Text;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Services;

/// <summary>
/// Provides functionality to read recipe files from the filesystem and deserialize them into domain entities.
/// </summary>
public class RecipeFileReader : IRecipeFileReader
{
    private readonly IRecipeSerializer _serializer;

    public RecipeFileReader(IRecipeSerializer serializer) => _serializer = serializer;

    /// <summary>
    /// Reads a recipe file from the specified path and deserializes its content.
    /// </summary>
    /// <param name="path">The file system path to the recipe file.</param>
    /// <param name="encoding">The text encoding to use. Defaults to UTF-8.</param>
    /// <returns>A <see cref="Result{T}"/> containing the recipe or an error.</returns>
    public Result<Recipe> ReadRecipe(string path, Encoding? encoding = null)
    {
        if (!File.Exists(path))
        {
            return Result.Fail(new RecipeError($"File not found: {path}"));
        }

        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new StreamReader(stream, encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true, throwOnInvalidBytes: true));
            return _serializer.Deserialize(reader);
        }
        catch (Exception ex)
        {
            return Result.Fail(new RecipeError($"Failed to read file: {path}").CausedBy(ex));
        }
    }
}