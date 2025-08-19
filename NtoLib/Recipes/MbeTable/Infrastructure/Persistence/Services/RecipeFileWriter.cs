#nullable enable

using System.Collections.Immutable;
using System.IO;
using System.Text;
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

    public IImmutableList<RecipeFileError> Write(Recipe recipe, string path, Encoding? encoding = null)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var tempPath = path + ".tmp";

        using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(fs, encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
        {
            var errors = _serializer.Serialize(recipe, writer);
            writer.Flush();
            fs.Flush(true);

            if (errors.Count > 0)
            {
                return errors;
            }
        }

        if (File.Exists(path))
        {
            File.Replace(tempPath, path, null);
        }
        else
        {
            File.Move(tempPath, path);
        }

        return ImmutableList<RecipeFileError>.Empty;
    }
}