using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;
using NtoLib.Recipes.MbeTable.ModuleConfig.Errors;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Validation;

public sealed class PropertyDefsValidator : ISectionValidator<YamlPropertyDefinition>
{
	private static readonly HashSet<string> SupportedSystemTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"System.String",
		"System.Int16",
		"System.Single",
	};

	private static readonly HashSet<string> SupportedFormatKinds = new(StringComparer.OrdinalIgnoreCase)
	{
		"Numeric",
		"Scientific",
		"TimeHms",
		"Int"
	};

	private static readonly HashSet<string> SpecialTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		PropertyTypeIds.Time,
		PropertyTypeIds.Enum
	};

	public Result Validate(IReadOnlyList<YamlPropertyDefinition> items)
	{
		var emptyCheck = ValidationCheck.NotEmpty(items, "PropertyDefs.yaml");
		if (emptyCheck.IsFailed)
			return emptyCheck;

		var errors = new List<ConfigError>();
		var uniqueIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var def in items)
		{
			var context = $"PropertyDefs.yaml, PropertyTypeId='{def.PropertyTypeId}'";

			AddIfFailed(ValidationCheck.NotEmpty(def.PropertyTypeId, context, "property_type_id"), errors);
			AddIfFailed(ValidationCheck.NotEmpty(def.SystemType, context, "system_type"), errors);

			if (!string.IsNullOrWhiteSpace(def.PropertyTypeId))
				AddIfFailed(
					ValidationCheck.Unique(def.PropertyTypeId, uniqueIds, "PropertyDefs.yaml", "property_type_id"),
					errors);

			AddIfFailed(ValidateTypeSupport(def, context), errors);
			AddIfFailed(ValidateFormatKind(def, context), errors);
		}

		return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
	}

	private static Result ValidateTypeSupport(YamlPropertyDefinition def, string context)
	{
		if (SpecialTypes.Contains(def.PropertyTypeId))
			return Result.Ok();

		var systemType = Type.GetType(def.SystemType, throwOnError: false, ignoreCase: true);
		if (systemType == null)
			return Result.Fail(ConfigPropertyErrors.UnknownSystemType(def.PropertyTypeId, def.SystemType, context));

		if (!SupportedSystemTypes.Contains(systemType.FullName ?? string.Empty))
		{
			var supported = string.Join(", ", SupportedSystemTypes);
			return Result.Fail(
				ConfigPropertyErrors.UnsupportedSystemType(def.PropertyTypeId, def.SystemType, supported, context));
		}

		return Result.Ok();
	}

	private static Result ValidateFormatKind(YamlPropertyDefinition def, string context)
	{
		if (string.IsNullOrWhiteSpace(def.FormatKind))
			return Result.Ok();

		if (!IsSupportedFormatKind(def.FormatKind))
			return CreateUnsupportedFormatKindError(def, context);

		var timeHmsCheck = ValidateTimeHmsFormat(def, context);
		if (timeHmsCheck.IsFailed)
			return timeHmsCheck;

		var scientificCheck = ValidateScientificFormat(def, context);
		if (scientificCheck.IsFailed)
			return scientificCheck;

		var intFormatCheck = ValidateIntFormat(def, context);
		if (intFormatCheck.IsFailed)
			return intFormatCheck;

		return Result.Ok();
	}

	private static bool IsSupportedFormatKind(string formatKind)
	{
		return SupportedFormatKinds.Contains(formatKind);
	}

	private static Result CreateUnsupportedFormatKindError(YamlPropertyDefinition def, string context)
	{
		var supported = string.Join(", ", SupportedFormatKinds);
		return Result.Fail(
			ConfigPropertyErrors.UnsupportedFormatKind(def.PropertyTypeId, def.FormatKind, supported, context));
	}

	private static Result ValidateTimeHmsFormat(YamlPropertyDefinition def, string context)
	{
		if (!IsTimeHmsFormat(def.FormatKind))
			return Result.Ok();

		if (!IsTimePropertyType(def.PropertyTypeId))
			return Result.Fail(ConfigPropertyErrors.TimeHmsRequiresTime(def.PropertyTypeId, context));

		return Result.Ok();
	}

	private static bool IsTimeHmsFormat(string formatKind)
	{
		return string.Equals(formatKind, "TimeHms", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsTimePropertyType(string propertyTypeId)
	{
		return string.Equals(propertyTypeId, PropertyTypeIds.Time, StringComparison.OrdinalIgnoreCase);
	}

	private static Result ValidateScientificFormat(YamlPropertyDefinition def, string context)
	{
		if (!IsScientificFormat(def.FormatKind))
			return Result.Ok();

		var systemType = Type.GetType(def.SystemType, throwOnError: false, ignoreCase: true);
		if (!IsNumericType(systemType))
			return Result.Fail(
				ConfigPropertyErrors.ScientificRequiresNumeric(def.PropertyTypeId, def.SystemType, context));

		return Result.Ok();
	}

	private static bool IsScientificFormat(string formatKind)
	{
		return string.Equals(formatKind, "Scientific", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsNumericType(Type? systemType)
	{
		return systemType == typeof(float) || systemType == typeof(short);
	}

	private static Result ValidateIntFormat(YamlPropertyDefinition def, string context)
	{
		if (!IsIntFormat(def.FormatKind))
			return Result.Ok();

		var systemType = Type.GetType(def.SystemType, throwOnError: false, ignoreCase: true);
		if (!IsFloatType(systemType))
		{
			var supported = "System.Single";
			return Result.Fail(
				ConfigPropertyErrors.UnsupportedFormatKind(def.PropertyTypeId, def.FormatKind, supported, context));
		}

		return Result.Ok();
	}

	private static bool IsIntFormat(string formatKind)
	{
		return string.Equals(formatKind, "Int", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsFloatType(Type? systemType)
	{
		return systemType == typeof(float);
	}

	private static void AddIfFailed(Result result, List<ConfigError> errors)
	{
		if (result.IsFailed)
		{
			foreach (var e in result.Errors)
				errors.Add(e as ConfigError ?? new ConfigError(e.Message, "PropertyDefs.yaml", "validation"));
		}
	}
}
