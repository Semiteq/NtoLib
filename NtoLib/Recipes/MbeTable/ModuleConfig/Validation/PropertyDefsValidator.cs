using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Validation;

/// <summary>
/// Validates property definitions from PropertyDefs.yaml.
/// Checks structure, uniqueness, supported types, and format_kind values.
/// </summary>
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
        "TimeHms"
    };

    private static readonly HashSet<string> SpecialTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        PropertyTypeIds.Time,
        PropertyTypeIds.Enum
    };

    public Result Validate(IReadOnlyList<YamlPropertyDefinition> items)
    {
        var checkEmpty = ValidationCheck.NotEmpty(items, "PropertyDefs.yaml");
        if (checkEmpty.IsFailed)
            return checkEmpty;

        return ValidateEachDefinition(items);
    }

    private static Result ValidateEachDefinition(IReadOnlyList<YamlPropertyDefinition> items)
    {
        var uniqueIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in items)
        {
            var context = $"PropertyDefs.yaml, PropertyTypeId='{def.PropertyTypeId}'";

            var structureResult = ValidateStructure(def, context);
            if (structureResult.IsFailed)
                return structureResult;

            var uniqueResult =
                ValidationCheck.Unique(def.PropertyTypeId, uniqueIds, "PropertyDefs.yaml", "property_type_id");
            if (uniqueResult.IsFailed)
                return uniqueResult;

            var typeResult = ValidateTypeSupport(def, context);
            if (typeResult.IsFailed)
                return typeResult;

            var formatResult = ValidateFormatKind(def, context);
            if (formatResult.IsFailed)
                return formatResult;
        }

        return Result.Ok();
    }

    private static Result ValidateStructure(YamlPropertyDefinition def, string context)
    {
        var propertyTypeIdCheck = ValidationCheck.NotEmpty(def.PropertyTypeId, context, "property_type_id");
        if (propertyTypeIdCheck.IsFailed)
            return propertyTypeIdCheck;

        var systemTypeCheck = ValidationCheck.NotEmpty(def.SystemType, context, "system_type");
        if (systemTypeCheck.IsFailed)
            return systemTypeCheck;

        return Result.Ok();
    }

    private static Result ValidateTypeSupport(YamlPropertyDefinition def, string context)
    {
        // Special types (Time, Enum) don't need SystemType validation
        if (SpecialTypes.Contains(def.PropertyTypeId))
            return Result.Ok();

        var systemType = Type.GetType(def.SystemType, throwOnError: false, ignoreCase: true);
        if (systemType == null)
        {
            return Result.Fail(new Error($"{context}: Unknown SystemType '{def.SystemType}'.")
                .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                .WithMetadata("PropertyTypeId", def.PropertyTypeId)
                .WithMetadata("SystemType", def.SystemType));
        }

        if (!SupportedSystemTypes.Contains(systemType.FullName ?? string.Empty))
        {
            var supported = string.Join(", ", SupportedSystemTypes);
            return Result.Fail(
                new Error($"{context}: Unsupported SystemType '{def.SystemType}'. Supported: {supported}.")
                    .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                    .WithMetadata("PropertyTypeId", def.PropertyTypeId)
                    .WithMetadata("SystemType", def.SystemType)
                    .WithMetadata("SupportedTypes", supported));
        }

        return Result.Ok();
    }

    private static Result ValidateFormatKind(YamlPropertyDefinition def, string context)
    {
        if (string.IsNullOrWhiteSpace(def.FormatKind))
        {
            // FormatKind is optional, Numeric is assumed if not specified.
            return Result.Ok();
        }

        if (!SupportedFormatKinds.Contains(def.FormatKind))
        {
            var supported = string.Join(", ", SupportedFormatKinds);
            return Result.Fail(
                new Error($"{context}: Unsupported format_kind '{def.FormatKind}'. Supported: {supported}.")
                    .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                    .WithMetadata("PropertyTypeId", def.PropertyTypeId)
                    .WithMetadata("FormatKind", def.FormatKind)
                    .WithMetadata("SupportedFormats", supported));
        }
        
        // TimeHms can only be used with PropertyTypeId='Time'
        if (string.Equals(def.FormatKind, "TimeHms", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(def.PropertyTypeId, PropertyTypeIds.Time, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Fail(new Error($"{context}: format_kind='TimeHms' can only be used with PropertyTypeId='Time'.")
                    .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                    .WithMetadata("PropertyTypeId", def.PropertyTypeId)
                    .WithMetadata("FormatKind", def.FormatKind));
            }
        }

        // Scientific can only be used with numeric types.
        if (string.Equals(def.FormatKind, "Scientific", StringComparison.OrdinalIgnoreCase))
        {
            var systemType = Type.GetType(def.SystemType, throwOnError: false, ignoreCase: true);
            if (systemType != typeof(float) && systemType != typeof(double) && 
                systemType != typeof(int) && systemType != typeof(short))
            {
                return Result.Fail(new Error($"{context}: format_kind='Scientific' can only be used with numeric types.")
                    .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                    .WithMetadata("PropertyTypeId", def.PropertyTypeId)
                    .WithMetadata("SystemType", def.SystemType)
                    .WithMetadata("FormatKind", def.FormatKind));
            }
        }

        
        return Result.Ok();
    }
}