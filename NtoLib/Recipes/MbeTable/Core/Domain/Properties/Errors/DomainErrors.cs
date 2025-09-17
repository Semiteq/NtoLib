#nullable enable
using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Errors;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;

/// <summary>
/// Represents a validation error (e.g., the value is out of the allowed Min/Max range).
/// </summary>
public class ValidationError : RecipeError
{
    public ValidationError(string message)
        : base(message, RecipeErrorCodes.PropertyValidationFailed)
    {
    }

    public ValidationError(IReadOnlyList<IError> errors) 
        : base(errors, RecipeErrorCodes.PropertyValidationFailed)
    {
        
    }
}

/// <summary>
/// Represents a type conversion error (e.g., failed to parse "abc" as a float).
/// </summary>
public class ConversionError : RecipeError
{
    public string InputValue { get; }
    public string TargetTypeName { get; }

    public ConversionError(string inputValue, string targetTypeName)
        : base($"Failed to convert '{inputValue}' to type {targetTypeName}.", RecipeErrorCodes.PropertyConversionFailed)
    {
        InputValue = inputValue;
        TargetTypeName = targetTypeName;
        WithMetadata(nameof(InputValue), inputValue);
        WithMetadata(nameof(TargetTypeName), targetTypeName);
    }
}

/// <summary>
/// Represents an error that occurs during a calculation operation.
/// </summary>
public class CalculationError : RecipeError
{
    public CalculationError(string details)
        : base($"Calculation error: {details}", RecipeErrorCodes.PropertyCalculationFailed)
    {
        WithMetadata("Details", details);
    }
}