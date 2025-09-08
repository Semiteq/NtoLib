#nullable enable

using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis;

/// <summary>
/// Represents the result of a loop validation operation.
/// </summary>
public record LoopValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyDictionary<int, int> NestingLevels { get; }

    // Non-nullable state
    public LoopValidationResult()
    {
        IsValid = true;
        NestingLevels = new Dictionary<int, int>();
        ErrorMessage = null;
    }
    
    public LoopValidationResult(IReadOnlyDictionary<int, int> nestingLevels)
    {
        IsValid = true;
        NestingLevels = nestingLevels;
        ErrorMessage = null;
    }

    public LoopValidationResult(string errorMessage)
    {
        IsValid = false;
        ErrorMessage = errorMessage;
        NestingLevels = new Dictionary<int, int>();
    }
}