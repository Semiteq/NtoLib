namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;

/// <summary>
/// Base abstract type for all predictable business errors related to property operations.
/// Using a sealed abstract record enables exhaustive checking in switch expressions.
/// </summary>
public abstract record RecipePropertyError(string Message);

/// <summary>
/// Represents a validation error (e.g., the value is out of the allowed Min/Max range).
/// </summary>
public record ValidationError(string Details) 
    : RecipePropertyError($"Validation failed: {Details}");

/// <summary>
/// Represents a type conversion error (e.g., failed to parse "abc" as a float).
/// </summary>
public record ConversionError(string Input, string TargetTypeName) 
    : RecipePropertyError($"Failed to convert '{Input}' to type {TargetTypeName}.");

/// <summary>
/// Represents an error that occurs during a calculation operation.
/// </summary>
public record CalculationError(string Details) 
    : RecipePropertyError($"Calculation error: {Details}");
        
/// <summary>
/// Represents an error that occurs when a null value is provided as input.
/// </summary>
public record NullInputError() 
    : RecipePropertyError("Input value cannot be null.");