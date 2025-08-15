#nullable enable
using System;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;

public sealed record RecipeFileError(
    int LineNumber,
    ColumnKey? Column,
    string Message,
    Exception? Exception = null);