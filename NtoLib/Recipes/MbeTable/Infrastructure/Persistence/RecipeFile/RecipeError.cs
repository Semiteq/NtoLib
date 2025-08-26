#nullable enable

using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;

/// <summary>
/// Represents a specific error that occurred while processing a recipe file.
/// It's designed to be used with the FluentResults library.
/// </summary>
public sealed class RecipeError : Error
{
    /// <summary>
    /// Gets the line number in the file where the error occurred.
    /// A value of 0 indicates a file-level error rather than a line-specific one.
    /// </summary>
    public int LineNumber { get; }

    /// <summary>
    /// Gets the key of the column associated with the error, if applicable.
    /// </summary>
    public ColumnKey? Column { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RecipeError"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="lineNumber">The line number where the error occurred.</param>
    /// <param name="column">The column key related to the error, if any.</param>
    public RecipeError(string message, int lineNumber = 0, ColumnKey? column = null)
        : base(message)
    {
        LineNumber = lineNumber;
        Column = column;

        Metadata.Add(nameof(LineNumber), lineNumber);
        if (column.HasValue)
        {
            Metadata.Add(nameof(Column), column.Value);
        }
    }
}