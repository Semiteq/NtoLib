#nullable enable

using System.Collections.Generic;
using FluentResults;

namespace NtoLib.Recipes.MbeTable.Errors;

/// <summary>
/// Base error for the recipe module, carrying a unified error code.
/// </summary>
public class RecipeError : Error
{
    /// <summary>
    /// Gets the unified error code.
    /// </summary>
    public RecipeErrorCodes Code { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RecipeError"/> class.
    /// </summary>
    /// <param name="message">Error message suitable for end users or logs.</param>
    /// <param name="code">Unified error code.</param>
    public RecipeError(string message, RecipeErrorCodes code) : base(message)
    {
        Code = code;
        WithMetadata(nameof(Code), code.ToString());
    }

    public RecipeError(IReadOnlyList<IError> errors, RecipeErrorCodes code) 
        : base("Following errors occured: " + string.Join("; ", errors))
    {
        Code = code;
        WithMetadata(nameof(Code), code.ToString());
    }
}