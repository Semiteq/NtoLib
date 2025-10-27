using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Common;

/// <summary>
/// Provides reusable validation helper methods for common validation scenarios.
/// All methods return Result for composition.
/// </summary>
public static class ValidationCheck
{
    /// <summary>
    /// Validates that a string value is not null or whitespace.
    /// </summary>
    public static Result NotEmpty(string? value, string context, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Fail(new Error($"{context}: Field '{fieldName}' is empty or missing.")
                .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("field", fieldName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates that an integer value is greater than zero.
    /// </summary>
    public static Result Positive(int value, string context, string fieldName)
    {
        if (value <= 0)
        {
            return Result.Fail(new Error($"{context}: Field '{fieldName}' must be greater than zero. Actual: {value}.")
                .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("field", fieldName)
                .WithMetadata("value", value.ToString()));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates that a value is unique within a set.
    /// </summary>
    public static Result Unique<T>(T value, HashSet<T> set, string context, string fieldName) where T : notnull
    {
        if (!set.Add(value))
        {
            return Result.Fail(new Error($"{context}: Duplicate value for '{fieldName}': '{value}'.")
                .WithMetadata(nameof(Codes), Codes.ConfigDuplicateValue)
                .WithMetadata("context", context)
                .WithMetadata("field", fieldName)
                .WithMetadata("value", value.ToString() ?? "null"));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates that a reference exists in a set.
    /// </summary>
    public static Result ReferenceExists<T>(T value, ISet<T> existingSet, string context, string fieldName)
    {
        if (!existingSet.Contains(value))
        {
            return Result.Fail(new Error($"{context}: Reference '{fieldName}' with value '{value}' does not exist.")
                .WithMetadata(nameof(Codes), Codes.ConfigMissingReference)
                .WithMetadata("context", context)
                .WithMetadata("field", fieldName)
                .WithMetadata("value", value?.ToString() ?? "null"));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates that a value is within a specified range (inclusive).
    /// </summary>
    public static Result InRange(int value, int min, int max, string context, string fieldName)
    {
        if (value < min || value > max)
        {
            return Result.Fail(new Error($"{context}: Field '{fieldName}' must be between {min} and {max}. Actual: {value}.")
                .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("field", fieldName)
                .WithMetadata("value", value.ToString())
                .WithMetadata("min", min.ToString())
                .WithMetadata("max", max.ToString()));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates that a string value matches one of the allowed enum values.
    /// </summary>
    public static Result InEnum<TEnum>(string? value, string context, string fieldName) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Fail(new Error($"{context}: Field '{fieldName}' is empty.")
                .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("field", fieldName));
        }

        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out _))
        {
            var allowedValues = string.Join(", ", Enum.GetNames(typeof(TEnum)));
            return Result.Fail(new Error($"{context}: Field '{fieldName}' has invalid value '{value}'. Allowed: {allowedValues}.")
                .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("field", fieldName)
                .WithMetadata("value", value)
                .WithMetadata("allowedValues", allowedValues));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates that a string value is one of the allowed values (case-insensitive).
    /// </summary>
    public static Result InSet(string? value, ISet<string> allowedValues, string context, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Fail(new Error($"{context}: Field '{fieldName}' is empty.")
                .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("field", fieldName));
        }

        if (!allowedValues.Contains(value))
        {
            var allowed = string.Join(", ", allowedValues);
            return Result.Fail(new Error($"{context}: Field '{fieldName}' has invalid value '{value}'. Allowed: {allowed}.")
                .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("field", fieldName)
                .WithMetadata("value", value)
                .WithMetadata("allowedValues", allowed));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates that a collection is not null or empty.
    /// </summary>
    public static Result NotEmpty<T>(IReadOnlyCollection<T>? collection, string context)
    {
        if (collection == null || collection.Count == 0)
        {
            return Result.Fail(new Error($"{context}: Collection is empty or null.")
                .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                .WithMetadata("context", context));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates that an object is not null.
    /// </summary>
    public static Result NotNull(object? value, string context, string fieldName)
    {
        if (value == null)
        {
            return Result.Fail(new Error($"{context}: Field '{fieldName}' is null.")
                .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("field", fieldName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates column width according to rules: > 2 or exactly -1.
    /// </summary>
    public static Result ValidateWidth(int width, string context, string columnKey, bool allowFullWidth = false)
    {
        if (width == -1)
        {
            if (!allowFullWidth)
            {
                // todo: remove?
                return Result.Fail(new Error($"{context}, Column '{columnKey}': Width=-1 is only allowed for 'comment' column.")
                    .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                    .WithMetadata("context", context)
                    .WithMetadata("columnKey", columnKey)
                    .WithMetadata("width", width.ToString()));
            }
            return Result.Ok();
        }

        if (width <= 2)
        {
            return Result.Fail(new Error($"{context}, Column '{columnKey}': Width must be > 2 or -1. Actual: {width}.")
                .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("columnKey", columnKey)
                .WithMetadata("width", width.ToString()));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates minimum width (must be > 2).
    /// </summary>
    public static Result ValidateMinWidth(int minWidth, string context, string columnKey)
    {
        if (minWidth <= 2)
        {
            return Result.Fail(new Error($"{context}, Column '{columnKey}': MinWidth must be > 2. Actual: {minWidth}.")
                .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("columnKey", columnKey)
                .WithMetadata("minWidth", minWidth.ToString()));
        }

        return Result.Ok();
    }
}