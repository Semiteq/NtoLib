using System;
using System.Collections.Generic;

using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Common;

/// <summary>
/// Reusable validation helpers returning Result for composition.
/// Errors are created in place without error codes; metadata is attached with context.
/// </summary>
public static class ValidationCheck
{
	public static Result NotEmpty(string? value, string context, string fieldName)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return Result.Fail(new ConfigError(
					$"Field '{fieldName}' is empty or missing.",
					section: ExtractSection(context),
					context: context)
				.WithDetail("field", fieldName));
		}

		return Result.Ok();
	}

	public static Result Positive(int value, string context, string fieldName)
	{
		if (value <= 0)
		{
			return Result.Fail(new ConfigError(
					$"Field '{fieldName}' must be greater than zero. Actual: {value}.",
					section: ExtractSection(context),
					context: context)
				.WithDetail("field", fieldName)
				.WithDetail("value", value));
		}

		return Result.Ok();
	}

	public static Result Unique<T>(T value, HashSet<T> set, string context, string fieldName) where T : notnull
	{
		if (!set.Add(value))
		{
			return Result.Fail(new ConfigError(
					$"Duplicate value for '{fieldName}': '{value}'.",
					section: ExtractSection(context),
					context: context)
				.WithDetail("field", fieldName)
				.WithDetail("value", value?.ToString()));
		}

		return Result.Ok();
	}

	public static Result ReferenceExists<T>(T value, ISet<T> existingSet, string context, string fieldName)
	{
		if (!existingSet.Contains(value))
		{
			return Result.Fail(new ConfigError(
					$"Reference '{fieldName}' with value '{value}' does not exist.",
					section: ExtractSection(context),
					context: context)
				.WithDetail("field", fieldName)
				.WithDetail("value", value?.ToString()));
		}

		return Result.Ok();
	}

	public static Result InRange(int value, int min, int max, string context, string fieldName)
	{
		if (value < min || value > max)
		{
			return Result.Fail(new ConfigError(
					$"Field '{fieldName}' must be between {min} and {max}. Actual: {value}.",
					section: ExtractSection(context),
					context: context)
				.WithDetail("field", fieldName)
				.WithDetail("value", value)
				.WithDetail("min", min)
				.WithDetail("max", max));
		}

		return Result.Ok();
	}

	public static Result InEnum<TEnum>(string? value, string context, string fieldName) where TEnum : struct, Enum
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return Result.Fail(new ConfigError(
					$"Field '{fieldName}' is empty.",
					section: ExtractSection(context),
					context: context)
				.WithDetail("field", fieldName));
		}

		if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out _))
		{
			var allowedValues = string.Join(", ", Enum.GetNames(typeof(TEnum)));
			return Result.Fail(new ConfigError(
					$"Field '{fieldName}' has invalid value '{value}'. Allowed: {allowedValues}.",
					section: ExtractSection(context),
					context: context)
				.WithDetail("field", fieldName)
				.WithDetail("value", value)
				.WithDetail("allowedValues", allowedValues));
		}

		return Result.Ok();
	}

	public static Result InSet(string? value, ISet<string> allowedValues, string context, string fieldName)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return Result.Fail(new ConfigError(
					$"Field '{fieldName}' is empty.",
					section: ExtractSection(context),
					context: context)
				.WithDetail("field", fieldName));
		}

		if (!allowedValues.Contains(value))
		{
			var allowed = string.Join(", ", allowedValues);
			return Result.Fail(new ConfigError(
					$"Field '{fieldName}' has invalid value '{value}'. Allowed: {allowed}.",
					section: ExtractSection(context),
					context: context)
				.WithDetail("field", fieldName)
				.WithDetail("value", value)
				.WithDetail("allowedValues", allowed));
		}

		return Result.Ok();
	}

	public static Result NotEmpty<T>(IReadOnlyCollection<T>? collection, string context)
	{
		if (collection == null || collection.Count == 0)
		{
			return Result.Fail(new ConfigError(
				"Collection is empty or null.",
				section: ExtractSection(context),
				context: context));
		}

		return Result.Ok();
	}

	public static Result NotNull(object? value, string context, string fieldName)
	{
		if (value == null)
		{
			return Result.Fail(new ConfigError(
					$"Field '{fieldName}' is null.",
					section: ExtractSection(context),
					context: context)
				.WithDetail("field", fieldName));
		}

		return Result.Ok();
	}

	public static Result ValidateWidth(int width, string context, string columnKey, bool allowFullWidth = false)
	{
		if (width == -1)
		{
			if (!allowFullWidth)
			{
				return Result.Fail(new ConfigError(
						"Width=-1 is only allowed for 'comment' column.",
						section: ExtractSection(context),
						context: context)
					.WithDetail("columnKey", columnKey)
					.WithDetail("width", width));
			}

			return Result.Ok();
		}

		if (width <= 2)
		{
			return Result.Fail(new ConfigError(
					$"Width must be > 2 or -1. Actual: {width}.",
					section: ExtractSection(context),
					context: context)
				.WithDetail("columnKey", columnKey)
				.WithDetail("width", width));
		}

		return Result.Ok();
	}

	public static Result ValidateMinWidth(int minWidth, string context, string columnKey)
	{
		if (minWidth <= 2)
		{
			return Result.Fail(new ConfigError(
					$"MinWidth must be > 2. Actual: {minWidth}.",
					section: ExtractSection(context),
					context: context)
				.WithDetail("columnKey", columnKey)
				.WithDetail("minWidth", minWidth));
		}

		return Result.Ok();
	}

	public static Result NonNegative(int value, string context, string fieldName)
	{
		if (value < 0)
		{
			return Result.Fail(new ConfigError(
					$"Field '{fieldName}' must be >= 0. Actual: {value}.",
					section: ExtractSection(context),
					context: context)
				.WithDetail("field", fieldName)
				.WithDetail("value", value));
		}

		return Result.Ok();
	}

	private static string ExtractSection(string context)
	{
		if (string.IsNullOrWhiteSpace(context))
			return string.Empty;

		var commaIdx = context.IndexOf(',');
		return commaIdx > 0 ? context.Substring(0, commaIdx).Trim() : context.Trim();
	}
}
