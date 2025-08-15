#nullable enable

using System.Collections.Immutable;
using System.IO;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;

public interface IRecipeSerializer
{
    (Recipe? Recipe, IImmutableList<RecipeFileError> Errors) Deserialize(TextReader reader);
    IImmutableList<RecipeFileError> Serialize(Recipe recipe, TextWriter writer);
}