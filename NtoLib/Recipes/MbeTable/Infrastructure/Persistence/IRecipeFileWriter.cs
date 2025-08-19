#nullable enable
using System.Collections.Immutable;
using System.Text;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence;

public interface IRecipeFileWriter
{
    IImmutableList<RecipeFileError> Write(Recipe recipe, string path, Encoding? encoding = null);
}