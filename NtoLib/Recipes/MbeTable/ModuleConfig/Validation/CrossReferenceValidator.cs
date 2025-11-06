using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Actions;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;
using NtoLib.Recipes.MbeTable.ModuleConfig.Errors;
using NtoLib.Recipes.MbeTable.ModuleConfig.YamlConfig;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Validation;

public sealed class CrossReferenceValidator
{
    private readonly INumberParser _parser;

    public CrossReferenceValidator(INumberParser parser)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    public Result Validate(CombinedYamlConfig files)
    {
        var errors = new List<ConfigError>();

        errors.AddRange(ValidateColumnToPropertyReferences(files));
        errors.AddRange(ValidateActionToColumnReferences(files));
        errors.AddRange(ValidateActionToPropertyReferences(files));
        errors.AddRange(ValidateActionToPinGroupReferences(files));
        errors.AddRange(ValidateActionDefaultValues(files));
        errors.AddRange(ValidateReadOnlyDefaultValueConflicts(files));

        return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
    }

    private static List<ConfigError> ValidateColumnToPropertyReferences(CombinedYamlConfig files)
    {
        var errors = new List<ConfigError>();
        var existingPropertyTypeIds = BuildPropertyTypeIdSet(files.PropertyDefs);

        foreach (var column in files.ColumnDefs.Items)
        {
            var context = $"ColumnDefs.yaml, Key='{column.Key}'";
            var propertyTypeId = column.BusinessLogic.PropertyTypeId;

            var validationResult = ValidationCheck.ReferenceExists(
                propertyTypeId,
                existingPropertyTypeIds,
                context,
                "property_type_id");

            Append(validationResult, errors, "ColumnDefs.yaml");
        }

        return errors;
    }

    private static List<ConfigError> ValidateActionToColumnReferences(CombinedYamlConfig files)
    {
        var errors = new List<ConfigError>();
        var existingColumnKeys = BuildColumnKeySet(files.ColumnDefs);

        foreach (var action in files.ActionDefs.Items)
        {
            var context = $"ActionsDefs.yaml, ActionId={action.Id}, ActionName='{action.Name}'";

            foreach (var column in action.Columns)
            {
                var validationResult = ValidationCheck.ReferenceExists(
                    column.Key,
                    existingColumnKeys,
                    context,
                    $"column key '{column.Key}'");

                Append(validationResult, errors, "ActionsDefs.yaml");
            }
        }

        return errors;
    }

    private static List<ConfigError> ValidateActionToPropertyReferences(CombinedYamlConfig files)
    {
        var errors = new List<ConfigError>();
        var existingPropertyTypeIds = BuildPropertyTypeIdSet(files.PropertyDefs);

        foreach (var action in files.ActionDefs.Items)
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

                Append(validationResult, errors, "ActionsDefs.yaml");
            }
        }

        return errors;
    }

    private static List<ConfigError> ValidateActionToPinGroupReferences(CombinedYamlConfig files)
    {
        var errors = new List<ConfigError>();
        var existingPinGroupNames = BuildPinGroupNameSet(files.PinGroupDefs);

        foreach (var action in files.ActionDefs.Items)
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

                Append(validationResult, errors, "ActionsDefs.yaml");
            }
        }

        return errors;
    }

    private List<ConfigError> ValidateActionDefaultValues(CombinedYamlConfig files)
    {
        var errors = new List<ConfigError>();
        var propertyDefinitionsByTypeId = BuildPropertyDefinitionDictionary(files.PropertyDefs);

        foreach (var action in files.ActionDefs.Items)
        {
            ValidateActionColumnsDefaultValues(action, propertyDefinitionsByTypeId, errors);
        }

        return errors;
    }

    private void ValidateActionColumnsDefaultValues(
        YamlActionDefinition action,
        Dictionary<string, YamlPropertyDefinition> propertyDefinitionsByTypeId,
        List<ConfigError> errors)
    {
        var context = $"ActionsDefs.yaml, ActionId={action.Id}, ActionName='{action.Name}'";

        foreach (var column in action.Columns)
        {
            ValidateColumnDefaultValue(column, propertyDefinitionsByTypeId, context, errors);
        }
    }

    private void ValidateColumnDefaultValue(
        YamlActionColumn column,
        Dictionary<string, YamlPropertyDefinition> propertyDefinitionsByTypeId,
        string actionContext,
        List<ConfigError> errors)
    {
        if (string.IsNullOrWhiteSpace(column.DefaultValue))
            return;

        if (!propertyDefinitionsByTypeId.TryGetValue(column.PropertyTypeId, out var propertyDef))
            return;

        var columnContext = $"{actionContext}, ColumnKey='{column.Key}'";

        var validationResult = ValidateDefaultValueAgainstPropertyDefinition(
            column.DefaultValue,
            propertyDef,
            columnContext);

        Append(validationResult, errors, "ActionsDefs.yaml");
    }

    private static List<ConfigError> ValidateReadOnlyDefaultValueConflicts(CombinedYamlConfig files)
    {
        var errors = new List<ConfigError>();

        var readOnlyColumns = files.ColumnDefs.Items
            .Where(c => c.BusinessLogic.ReadOnly)
            .Select(c => c.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var action in files.ActionDefs.Items)
        {
            var context = $"ActionsDefs.yaml, ActionId={action.Id}, ActionName='{action.Name}'";

            foreach (var column in action.Columns)
            {
                if (string.IsNullOrWhiteSpace(column.DefaultValue))
                    continue;

                if (readOnlyColumns.Contains(column.Key))
                    errors.Add(CrossRefErrors.ReadOnlyDefaultConflict($"{context}, ColumnKey='{column.Key}'",
                        column.Key));
            }
        }

        return errors;
    }

    private Result ValidateDefaultValueAgainstPropertyDefinition(
        string defaultValue,
        YamlPropertyDefinition propertyDefinition,
        string context)
    {
        if (IsSpecialPropertyType(propertyDefinition.PropertyTypeId))
            return Result.Ok();

        var systemType = GetSystemType(propertyDefinition.SystemType);
        if (systemType == null)
            return Result.Ok();

        return ValidateDefaultValueBySystemType(defaultValue, systemType, propertyDefinition, context);
    }

    private static Type? GetSystemType(string systemTypeName)
    {
        return Type.GetType(systemTypeName, throwOnError: false, ignoreCase: true);
    }

    private Result ValidateDefaultValueBySystemType(
        string defaultValue,
        Type systemType,
        YamlPropertyDefinition propertyDefinition,
        string context)
    {
        if (systemType == typeof(string))
            return ValidateStringDefaultValue(defaultValue, propertyDefinition, context);

        if (systemType == typeof(short))
            return ValidateInt16DefaultValue(defaultValue, propertyDefinition, context);

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
            return Result.Fail(CrossRefErrors.DefaultValueExceedsMaxLength(context, defaultValue.Length,
                propertyDefinition.MaxLength.Value, defaultValue));

        return Result.Ok();
    }

    private Result ValidateInt16DefaultValue(
        string defaultValue,
        YamlPropertyDefinition propertyDefinition,
        string context)
    {
        if (!TryParseInt16Value(defaultValue, out var shortValue))
            return Result.Fail(CrossRefErrors.DefaultValueNotInt16(context, defaultValue));

        return ValidateNumericRange(shortValue, propertyDefinition, context);
    }

    private bool TryParseInt16Value(string value, out short result)
    {
        return _parser.TryParseInt16(value, out result);
    }

    private Result ValidateFloatDefaultValue(
        string defaultValue,
        YamlPropertyDefinition propertyDefinition,
        string context)
    {
        if (!TryParseFloatValue(defaultValue, out var parsedValue))
            return Result.Fail(CrossRefErrors.DefaultValueNotFloat(context, defaultValue));

        return ValidateNumericRange(parsedValue, propertyDefinition, context);
    }

    private bool TryParseFloatValue(string value, out float result)
    {
        return _parser.TryParseSingle(value, out result);
    }

    private static Result ValidateNumericRange(float parsedValue, YamlPropertyDefinition propertyDefinition,
        string context)
    {
        if (IsValueBelowMinimum(parsedValue, propertyDefinition))
            return Result.Fail(
                CrossRefErrors.DefaultValueLessThanMin(context, parsedValue, propertyDefinition.Min!.Value));

        if (IsValueAboveMaximum(parsedValue, propertyDefinition))
            return Result.Fail(
                CrossRefErrors.DefaultValueExceedsMax(context, parsedValue, propertyDefinition.Max!.Value));

        return Result.Ok();
    }

    private static bool IsValueBelowMinimum(float value, YamlPropertyDefinition propertyDefinition)
    {
        return propertyDefinition.Min.HasValue && value < propertyDefinition.Min.Value;
    }

    private static bool IsValueAboveMaximum(float value, YamlPropertyDefinition propertyDefinition)
    {
        return propertyDefinition.Max.HasValue && value > propertyDefinition.Max.Value;
    }

    private static HashSet<string> BuildPropertyTypeIdSet(PropertyDefsYamlConfig yamlConfig) =>
        yamlConfig.Items.Select(p => p.PropertyTypeId).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static HashSet<string> BuildColumnKeySet(ColumnDefsYamlConfig yamlConfig) =>
        yamlConfig.Items.Select(c => c.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static HashSet<string> BuildPinGroupNameSet(PinGroupDefsYamlConfig yamlConfig) =>
        yamlConfig.Items.Select(g => g.GroupName).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, YamlPropertyDefinition> BuildPropertyDefinitionDictionary(
        PropertyDefsYamlConfig yamlConfig) =>
        yamlConfig.Items.ToDictionary(p => p.PropertyTypeId, StringComparer.OrdinalIgnoreCase);

    private static void Append(Result result, List<ConfigError> errors, string defaultSection)
    {
        if (result.IsFailed)
        {
            foreach (var e in result.Errors)
                errors.Add(e as ConfigError ?? new ConfigError(e.Message, defaultSection, "validation"));
        }
    }
}