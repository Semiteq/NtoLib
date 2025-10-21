using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Validation;

/// <summary>
/// Validates column definitions from ColumnDefs.yaml.
/// Checks mandatory columns, supported column types, width/min_width constraints,
/// and action_combo_box requirements.
/// </summary>
public sealed class ColumnDefsValidator : ISectionValidator<YamlColumnDefinition>
{
    private static readonly List<ColumnIdentifier> ConfigMandatoryColumns = new()
    {
        MandatoryColumns.Action,
        MandatoryColumns.StepDuration,
        MandatoryColumns.StepStartTime,
        MandatoryColumns.Comment
    };

    private static readonly HashSet<string> SupportedColumnTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ColumnTypeIds.ActionComboBox,
        ColumnTypeIds.ActionTargetComboBox,
        ColumnTypeIds.PropertyField,
        ColumnTypeIds.StepStartTimeField,
        ColumnTypeIds.TextField
    };

    public Result Validate(IReadOnlyList<YamlColumnDefinition> items)
    {
        var checkEmpty = ValidationCheck.NotEmpty(items, "ColumnDefs.yaml");
        if (checkEmpty.IsFailed)
            return checkEmpty;

        var mandatoryResult = ValidateMandatoryColumns(items);
        if (mandatoryResult.IsFailed)
            return mandatoryResult;

        var columnTypesResult = ValidateSupportedColumnTypes(items);
        if (columnTypesResult.IsFailed)
            return columnTypesResult;

        return ValidateEachDefinition(items);
    }

    private static Result ValidateMandatoryColumns(IReadOnlyList<YamlColumnDefinition> items)
    {
        var definedKeys = new HashSet<string>(items.Select(d => d.Key), StringComparer.OrdinalIgnoreCase);
        var missingKeys = ConfigMandatoryColumns
            .Where(mc => !definedKeys.Contains(mc.Value))
            .Select(mc => mc.Value)
            .ToList();

        if (missingKeys.Any())
        {
            var missing = string.Join(", ", missingKeys);
            return Result.Fail(new Error($"ColumnDefs.yaml: Missing mandatory columns: {missing}.")
                .WithMetadata("code", Codes.ConfigInvalidSchema)
                .WithMetadata("MissingColumns", missing));
        }

        return Result.Ok();
    }

    private static Result ValidateSupportedColumnTypes(IReadOnlyList<YamlColumnDefinition> items)
    {
        var unsupported = items
            .Where(d => !string.IsNullOrWhiteSpace(d.Ui?.ColumnType) &&
                        !SupportedColumnTypes.Contains(d.Ui.ColumnType))
            .Select(d => new { d.Key, ColumnType = d.Ui?.ColumnType })
            .ToList();

        if (unsupported.Any())
        {
            var info = string.Join(", ", unsupported.Select(c => $"'{c.Key}' ({c.ColumnType})"));
            return Result.Fail(new Error($"ColumnDefs.yaml: Unsupported column types: {info}.")
                .WithMetadata("code", Codes.ConfigInvalidSchema)
                .WithMetadata("UnsupportedColumns", info));
        }

        return Result.Ok();
    }

    private static Result ValidateEachDefinition(IReadOnlyList<YamlColumnDefinition> items)
    {
        var uniqueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var col in items)
        {
            var context = $"ColumnDefs.yaml, Key='{col.Key}'";

            var structureResult = ValidateStructure(col, context);
            if (structureResult.IsFailed)
                return structureResult;

            var uniqueResult = ValidationCheck.Unique(col.Key, uniqueKeys, "ColumnDefs.yaml", "key");
            if (uniqueResult.IsFailed)
                return uniqueResult;

            var widthResult = ValidateWidthConstraints(col, context);
            if (widthResult.IsFailed)
                return widthResult;

            var dropdownResult = ValidateMaxDropdownItems(col, context);
            if (dropdownResult.IsFailed)
                return dropdownResult;

            var actionComboResult = ValidateActionComboBox(col, context);
            if (actionComboResult.IsFailed)
                return actionComboResult;
        }

        return Result.Ok();
    }

    private static Result ValidateStructure(YamlColumnDefinition col, string context)
    {
        var keyCheck = ValidationCheck.NotEmpty(col.Key, context, "key");
        if (keyCheck.IsFailed)
            return keyCheck;

        var uiCheck = ValidationCheck.NotNull(col.Ui, context, "ui");
        if (uiCheck.IsFailed)
            return uiCheck;

        var businessLogicCheck = ValidationCheck.NotNull(col.BusinessLogic, context, "business_logic");
        if (businessLogicCheck.IsFailed)
            return businessLogicCheck;

        var propertyTypeIdCheck = ValidationCheck.NotEmpty(col.BusinessLogic.PropertyTypeId, context, "property_type_id");
        if (propertyTypeIdCheck.IsFailed)
            return propertyTypeIdCheck;

        return Result.Ok();
    }

    private static Result ValidateWidthConstraints(YamlColumnDefinition col, string context)
    {
        var allowFullWidth = string.Equals(col.Key, "comment", StringComparison.OrdinalIgnoreCase);
        var widthCheck = ValidationCheck.ValidateWidth(col.Ui.Width, context, col.Key, allowFullWidth);
        if (widthCheck.IsFailed)
            return widthCheck;

        var minWidthCheck = ValidationCheck.ValidateMinWidth(col.Ui.MinWidth, context, col.Key);
        if (minWidthCheck.IsFailed)
            return minWidthCheck;

        return Result.Ok();
    }

    private static Result ValidateMaxDropdownItems(YamlColumnDefinition col, string context)
    {
        if (col.Ui.MaxDropdownItems <= 0)
        {
            return Result.Fail(new Error($"{context}: max_dropdown_items must be > 0. Actual: {col.Ui.MaxDropdownItems}.")
                .WithMetadata("code", Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("columnKey", col.Key)
                .WithMetadata("maxDropdownItems", col.Ui.MaxDropdownItems.ToString()));
        }

        return Result.Ok();
    }

    private static Result ValidateActionComboBox(YamlColumnDefinition col, string context)
    {
        if (!string.Equals(col.Ui.ColumnType, ColumnTypeIds.ActionComboBox, StringComparison.OrdinalIgnoreCase))
            return Result.Ok();

        // If column_type is action_combo_box, key must be "action"
        if (!string.Equals(col.Key, "action", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Fail(new Error($"{context}: column_type 'action_combo_box' can only be used with key='action'.")
                .WithMetadata("code", Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("columnKey", col.Key)
                .WithMetadata("columnType", col.Ui.ColumnType));
        }

        return Result.Ok();
    }
}