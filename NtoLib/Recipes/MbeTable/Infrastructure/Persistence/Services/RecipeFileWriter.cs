#nullable enable

using System;
using System.IO;
using System.Text;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Services;

/// <summary>
/// Responsible for writing serialized representations of recipes to files.
/// </summary>
public sealed class RecipeFileWriter : IRecipeFileWriter
{
    private readonly IRecipeSerializer _serializer;
    public RecipeFileWriter(IRecipeSerializer serializer) => _serializer = serializer;

    /// <summary>
    /// Writes a recipe to the specified path.
    /// </summary>
    /// <param name="recipe">The recipe to write.</param>
    /// <param name="path">The file system path to write to.</param>
    /// <param name="encoding">The text encoding to use. Defaults to UTF-8 with BOM.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result WriteRecipe(Recipe recipe, string path, Encoding? encoding = null)
    {
        var tempPath = path + ".tmp";

        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(fs, encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
            {
                var serializeResult = _serializer.Serialize(recipe, writer);
                if (serializeResult.IsFailed)
                {
                    return serializeResult;
                }
                
                writer.Flush();
                fs.Flush(true);
            }

            if (File.Exists(path))
            {
                File.Replace(tempPath, path, null);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }
        catch (Exception ex)
        {
            return Result.Fail(new RecipeError($"Failed to write file to '{path}'").CausedBy(ex));
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // suppress
                }
            }
        }

        return Result.Ok();
    }
}