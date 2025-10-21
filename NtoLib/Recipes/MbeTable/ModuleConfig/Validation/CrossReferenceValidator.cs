using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;
using NtoLib.Recipes.MbeTable.ModuleConfig.Sections;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Validation;

/// <summary>
/// Validates cross-references between configuration sections.
/// Ensures that all references (PropertyTypeId, ColumnKey, GroupName) exist
/// and that default values are compatible with their property definitions.
/// </summary>
public sealed class CrossReferenceValidator
{
    public Result Validate(ConfigurationSections sections)
    {
        var columnToPropertyResult = ValidateColumnToPropertyReferences(sections);
        if (columnToPropertyResult.IsFailed)
            return columnToPropertyResult;

        var actionToColumnResult = ValidateActionToColumnReferences(sections);
        if (actionToColumnResult.IsFailed)
            return actionToColumnResult;

        var actionToPropertyResult = ValidateActionToPropertyReferences(sections);
        if (actionToPropertyResult.IsFailed)
            return actionToPropertyResult;

        var actionToPinGroupResult = ValidateActionToPinGroupReferences(sections);
        if (actionToPinGroupResult.IsFailed)
            return actionToPinGroupResult;

        var defaultValueResult = ValidateActionDefaultValues(sections);
        if (defaultValueResult.IsFailed)
            return defaultValueResult;

        var readOnlyDefaultResult = ValidateReadOnlyDefaultValueConflicts(sections);
        if (readOnlyDefaultResult.IsFailed)
            return readOnlyDefaultResult;

        return Result.Ok();
    }

    private static Result ValidateColumnToPropertyReferences(ConfigurationSections sections)
    {
        var existingPropertyTypeIds = BuildPropertyTypeIdSet(sections.PropertyDefs);

        foreach (var column in sections.ColumnDefs.Items)
        {
            var context = $"ColumnDefs.yaml, Key='{column.Key}'";
            var propertyTypeId = column.BusinessLogic.PropertyTypeId;

            var validationResult = ValidationCheck.ReferenceExists(
                propertyTypeId,
                existingPropertyTypeIds,
                context,
                "property_type_id");

            if (validationResult.IsFailed)
                return validationResult;
        }

        return Result.Ok();
    }

    private static Result ValidateActionToColumnReferences(ConfigurationSections sections)
    {
        var existingColumnKeys = BuildColumnKeySet(sections.ColumnDefs);

        foreach (var action in sections.ActionDefs.Items)
        {
            var context = $"ActionsDefs.yaml, ActionId={action.Id}, ActionName='{action.Name}'";

            foreach (var column in action.Columns)
            {
                var validationResult = ValidationCheck.ReferenceExists(
                    column.Key,
                    existingColumnKeys,
                    context,
                    $"column key '{column.Key}'");

                if (validationResult.IsFailed)
                    return validationResult;
            }
        }

        return Result.Ok();
    }

    private static Result ValidateActionToPropertyReferences(ConfigurationSections sections)
    {
        var existingPropertyTypeIds = BuildPropertyTypeIdSet(sections.PropertyDefs);

        foreach (var action in sections.ActionDefs.Items)
        {
            var context = $"ActionsDefs.yaml, ActionId={action.Id}, ActionName='{action.Name}'";

            foreach (var column in action.Columns)
            {
                var columnContext = $"{context}, ColumnKey='{column.Key}'";

                var validationResult = ValidationCheck.ReferenceExists(
                    column.PropertyTypeId,
                    existingPropertyTypeIds,
                    columnContext,
                    "property_type_id");

                if (validationResult.IsFailed)
                    return validationResult;
            }
        }

        return Result.Ok();
    }

    private static Result ValidateActionToPinGroupReferences(ConfigurationSections sections)
    {
        var existingPinGroupNames = BuildPinGroupNameSet(sections.PinGroupDefs);

        foreach (var action in sections.ActionDefs.Items)
        {
            var context = $"ActionsDefs.yaml, ActionId={action.Id}, ActionName='{action.Name}'";

            foreach (var column in action.Columns)
            {
                if (string.IsNullOrWhiteSpace(column.GroupName))
                    continue;

                var columnContext = $"{context}, ColumnKey='{column.Key}'";

                var validationResult = ValidationCheck.ReferenceExists(
                    column.GroupName,
                    existingPinGroupNames,
                    columnContext,
                    "group_name");

                if (validationResult.IsFailed)
                    return validationResult;
            }
        }

        return Result.Ok();
    }

    private static Result ValidateActionDefaultValues(ConfigurationSections sections)
    {
        var propertyDefinitionsByTypeId = BuildPropertyDefinitionDictionary(sections.PropertyDefs);

        foreach (var action in sections.ActionDefs.Items)
        {
            var context = $"ActionsDefs.yaml, ActionId={action.Id}, ActionName='{action.Name}'";

            foreach (var column in action.Columns)
            {
                if (string.IsNullOrWhiteSpace(column.DefaultValue))
                    continue;

                if (!propertyDefinitionsByTypeId.TryGetValue(column.PropertyTypeId, out var propertyDef))
                    continue;

                var columnContext = $"{context}, ColumnKey='{column.Key}'";

                var validationResult = ValidateDefaultValueAgainstPropertyDefinition(
                    column.DefaultValue,
                    propertyDef,
                    columnContext);

                if (validationResult.IsFailed)
                    return validationResult;
            }
        }

        return Result.Ok();
    }

    private static Result ValidateReadOnlyDefaultValueConflicts(ConfigurationSections sections)
    {
        var readOnlyColumns = sections.ColumnDefs.Items
            .Where(c => c.BusinessLogic.ReadOnly)
            .Select(c => c.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var action in sections.ActionDefs.Items)
        {
            var context = $"ActionsDefs.yaml, ActionId={action.Id}, ActionName='{action.Name}'";

            foreach (var column in action.Columns)
            {
                if (string.IsNullOrWhiteSpace(column.DefaultValue))
                    continue;

                if (readOnlyColumns.Contains(column.Key))
                {
                    return Result.Fail(new Error($"{context}, ColumnKey='{column.Key}': Cannot set default_value for read_only column.")
                        .WithMetadata("code", Codes.ConfigInvalidSchema)
                        .WithMetadata("context", context)
                        .WithMetadata("columnKey", column.Key));
                }
            }
        }

        return Result.Ok();
    }

    private static Result ValidateDefaultValueAgainstPropertyDefinition(
        string defaultValue,
        YamlPropertyDefinition propertyDefinition,
        string context)
    {
        // Skip validation for special types (Enum, Time)
        if (IsSpecialPropertyType(propertyDefinition.PropertyTypeId))
            return Result.Ok();

        var systemType = Type.GetType(propertyDefinition.SystemType, throwOnError: false, ignoreCase: true);
        if (systemType == null)
            return Result.Ok();

        if (systemType == typeof(string))
            return ValidateStringDefaultValue(defaultValue, propertyDefinition, context);

        if (systemType == typeof(short) || systemType == typeof(int))
            return ValidateIntegerDefaultValue(defaultValue, propertyDefinition, context, systemType);

        if (systemType == typeof(float))
            return ValidateFloatDefaultValue(defaultValue, propertyDefinition, context);

        return Result.Ok();
    }

    private static bool IsSpecialPropertyType(string propertyTypeId)
    {
        return string.Equals(propertyTypeId, PropertyTypeIds.Enum, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(propertyTypeId, PropertyTypeIds.Time, StringComparison.OrdinalIgnoreCase);
    }

    private static Result ValidateStringDefaultValue(
        string defaultValue,
        YamlPropertyDefinition propertyDefinition,
        string context)
    {
        if (propertyDefinition.MaxLength.HasValue && defaultValue.Length > propertyDefinition.MaxLength.Value)
        {
            return Result.Fail(new Error($"{context}: default_value exceeds max_length. Value length: {defaultValue.Length}, MaxLength: {propertyDefinition.MaxLength.Value}.")
                .WithMetadata("code", Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("defaultValue", defaultValue)
                .WithMetadata("maxLength", propertyDefinition.MaxLength.Value.ToString()));
        }

        return Result.Ok();
    }

    private static Result ValidateIntegerDefaultValue(
        string defaultValue,
        YamlPropertyDefinition propertyDefinition,
        string context,
        Type systemType)
    {
        float parsedValue;

        if (systemType == typeof(short))
        {
            if (!short.TryParse(defaultValue, out var shortValue))
            {
                return Result.Fail(new Error($"{context}: default_value '{defaultValue}' is not a valid Int16.")
                    .WithMetadata("code", Codes.ConfigInvalidSchema)
                    .WithMetadata("context", context)
                    .WithMetadata("defaultValue", defaultValue));
            }
            parsedValue = shortValue;
        }
        else // int
        {
            if (!int.TryParse(defaultValue, out var intValue))
            {
                return Result.Fail(new Error($"{context}: default_value '{defaultValue}' is not a valid Int32.")
                    .WithMetadata("code", Codes.ConfigInvalidSchema)
                    .WithMetadata("context", context)
                    .WithMetadata("defaultValue", defaultValue));
            }
            parsedValue = intValue;
        }

        return ValidateNumericRange(parsedValue, propertyDefinition, context);
    }

    private static Result ValidateFloatDefaultValue(
        string defaultValue,
        YamlPropertyDefinition propertyDefinition,
        string context)
    {
        if (!float.TryParse(defaultValue, out var parsedValue))
        {
            return Result.Fail(new Error($"{context}: default_value '{defaultValue}' is not a valid Float.")
                .WithMetadata("code", Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("defaultValue", defaultValue));
        }

        return ValidateNumericRange(parsedValue, propertyDefinition, context);
    }

    private static Result ValidateNumericRange(float parsedValue, YamlPropertyDefinition propertyDefinition, string context)
    {
        if (propertyDefinition.Min.HasValue && parsedValue < propertyDefinition.Min.Value)
        {
            return Result.Fail(new Error($"{context}: default_value {parsedValue} is less than min {propertyDefinition.Min.Value}.")
                .WithMetadata("code", Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("defaultValue", parsedValue.ToString())
                .WithMetadata("min", propertyDefinition.Min.Value.ToString()));
        }

        if (propertyDefinition.Max.HasValue && parsedValue > propertyDefinition.Max.Value)
        {
            return Result.Fail(new Error($"{context}: default_value {parsedValue} exceeds max {propertyDefinition.Max.Value}.")
                .WithMetadata("code", Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("defaultValue", parsedValue.ToString())
                .WithMetadata("max", propertyDefinition.Max.Value.ToString()));
        }

        return Result.Ok();
    }

    private static HashSet<string> BuildPropertyTypeIdSet(PropertyDefsSection section)
    {
        return section.Items
            .Select(p => p.PropertyTypeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> BuildColumnKeySet(ColumnDefsSection section)
    {
        return section.Items
            .Select(c => c.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> BuildPinGroupNameSet(PinGroupDefsSection section)
    {
        return section.Items
            .Select(g => g.GroupName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, YamlPropertyDefinition> BuildPropertyDefinitionDictionary(PropertyDefsSection section)
    {
        return section.Items
            .ToDictionary(p => p.PropertyTypeId, StringComparer.OrdinalIgnoreCase);
    }
}