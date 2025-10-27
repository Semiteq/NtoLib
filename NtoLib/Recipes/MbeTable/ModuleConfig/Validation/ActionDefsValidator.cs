using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Actions;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Validation;

/// <summary>
/// Validates action definitions from ActionsDefs.yaml.
/// Checks structure, uniqueness, deploy_duration rules, default_value constraints,
/// and LongLasting action requirements.
/// </summary>
public sealed class ActionDefsValidator : ISectionValidator<YamlActionDefinition>
{
    public Result Validate(IReadOnlyList<YamlActionDefinition> items)
    {
        var checkEmpty = ValidationCheck.NotEmpty(items, "ActionsDefs.yaml");
        if (checkEmpty.IsFailed)
            return checkEmpty;

        return ValidateEachAction(items);
    }

    private static Result ValidateEachAction(IReadOnlyList<YamlActionDefinition> items)
    {
        var uniqueIds = new HashSet<int>();

        foreach (var action in items)
        {
            var context = $"ActionsDefs.yaml, ActionId={action.Id}, ActionName='{action.Name}'";

            var structureResult = ValidateActionStructure(action, context);
            if (structureResult.IsFailed)
                return structureResult;

            var uniqueResult = ValidationCheck.Unique(action.Id, uniqueIds, "ActionsDefs.yaml", "id");
            if (uniqueResult.IsFailed)
                return uniqueResult;

            var deployDurationResult = ValidateDeployDuration(action, context);
            if (deployDurationResult.IsFailed)
                return deployDurationResult;

            var columnsResult = ValidateActionColumns(action, context);
            if (columnsResult.IsFailed)
                return columnsResult;

            var longLastingResult = ValidateLongLastingRequirements(action, context);
            if (longLastingResult.IsFailed)
                return longLastingResult;
        }

        return Result.Ok();
    }

    private static Result ValidateActionStructure(YamlActionDefinition action, string context)
    {
        var idCheck = ValidationCheck.Positive(action.Id, context, "id");
        if (idCheck.IsFailed)
            return idCheck;

        var nameCheck = ValidationCheck.NotEmpty(action.Name, context, "name");
        if (nameCheck.IsFailed)
            return nameCheck;

        var columnsCheck = ValidationCheck.NotNull(action.Columns, context, "columns");
        if (columnsCheck.IsFailed)
            return columnsCheck;

        return Result.Ok();
    }

    private static Result ValidateDeployDuration(YamlActionDefinition action, string context)
    {
        var durationCheck = ValidationCheck.NotEmpty(action.DeployDuration, context, "deploy_duration");
        if (durationCheck.IsFailed)
            return durationCheck;

        if (!Enum.TryParse<DeployDuration>(action.DeployDuration, ignoreCase: true, out _))
        {
            return Result.Fail(new Error($"{context}: Unknown deploy_duration '{action.DeployDuration}'. Allowed: Immediate, LongLasting.")
                .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("deployDuration", action.DeployDuration));
        }

        return Result.Ok();
    }

    private static Result ValidateActionColumns(YamlActionDefinition action, string context)
    {
        var uniqueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in action.Columns)
        {
            var columnContext = $"{context}, ColumnKey='{column.Key}'";

            var keyCheck = ValidationCheck.NotEmpty(column.Key, columnContext, "key");
            if (keyCheck.IsFailed)
                return keyCheck;

            var propertyTypeCheck = ValidationCheck.NotEmpty(column.PropertyTypeId, columnContext, "property_type_id");
            if (propertyTypeCheck.IsFailed)
                return propertyTypeCheck;

            var uniqueCheck = ValidationCheck.Unique(column.Key, uniqueKeys, context, $"column key '{column.Key}'");
            if (uniqueCheck.IsFailed)
                return uniqueCheck;

            var enumCheck = ValidateEnumColumnHasGroupName(column, columnContext);
            if (enumCheck.IsFailed)
                return enumCheck;
        }

        return Result.Ok();
    }

    private static Result ValidateEnumColumnHasGroupName(YamlActionColumn column, string context)
    {
        if (!string.Equals(column.PropertyTypeId, PropertyTypeIds.Enum, StringComparison.OrdinalIgnoreCase))
            return Result.Ok();

        if (string.IsNullOrWhiteSpace(column.GroupName))
        {
            return Result.Fail(new Error($"{context}: Column with property_type_id='Enum' must have a 'group_name'.")
                .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("columnKey", column.Key)
                .WithMetadata("propertyTypeId", column.PropertyTypeId));
        }

        return Result.Ok();
    }

    private static Result ValidateLongLastingRequirements(YamlActionDefinition action, string context)
    {
        if (!string.Equals(action.DeployDuration, "LongLasting", StringComparison.OrdinalIgnoreCase))
            return Result.Ok();

        var hasStepDuration = action.Columns.Any(c =>
            string.Equals(c.Key, "step_duration", StringComparison.OrdinalIgnoreCase));

        if (!hasStepDuration)
        {
            return Result.Fail(new Error($"{context}: Action with deploy_duration='LongLasting' must include column 'step_duration'.")
                .WithMetadata(nameof(Codes), Codes.ConfigInvalidSchema)
                .WithMetadata("context", context)
                .WithMetadata("deployDuration", action.DeployDuration));
        }

        return Result.Ok();
    }
}