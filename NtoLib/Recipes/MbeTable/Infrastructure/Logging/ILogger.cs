#nullable enable

using System;
using System.Runtime.CompilerServices;
using NtoLib.Recipes.MbeTable.Errors;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Logging;

public interface ILogger
{
    void Log(string message, [CallerMemberName] string caller = "");
    
    /// <summary>
    /// Logs a structured <see cref="RecipeError"/> with accumulated context.
    /// Use this for business and presentation errors that require structured tracking.
    /// </summary>
    /// <param name="error">The structured error object containing code and context.</param>
    /// <param name="caller">Auto-captured caller member name.</param>
    void LogError(RecipeError error, [CallerMemberName] string caller = "");
    
    void LogException(Exception ex, object? contextData = null, [CallerMemberName] string caller = "");
}