#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Errors;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Validators;

/// <summary>
/// Validates action definitions from configuration.
/// </summary>
public sealed class ActionDefsValidator : IActionDefsValidator
{
    /// <inheritdoc />
    public Result Validate(IReadOnlyCollection<ActionDefinition> definitions, TableColumns tableColumns)
    {
        if (definitions == null || definitions.Count == 0)
            return Result.Fail(new RecipeError("ActionsDefs.yaml is empty or invalid.", RecipeErrorCodes.ConfigInvalidSchema));

        var uniqueIds = new HashSet<int>();
        var allColumnKeys = tableColumns.GetColumns()
            .Select(c => c.Key.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var action in definitions)
        {
            if (!uniqueIds.Add(action.Id))
                return Result.Fail(new RecipeError($"In ActionsDefs.yaml, duplicate Action ID detected: {action.Id}", RecipeErrorCodes.ConfigDuplicateId));
            
            if (string.IsNullOrWhiteSpace(action.Name))
                return Result.Fail(new RecipeError($"In ActionsDefs.yaml, action with ID {action.Id} has an empty name.", RecipeErrorCodes.ConfigInvalidSchema));

            if (action.Columns is null)
                return Result.Fail(new RecipeError($"In ActionsDefs.yaml, action {action.Id} ('{action.Name}') has a null 'Columns' collection.", RecipeErrorCodes.ConfigInvalidSchema));

            var uniqueColumnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in action.Columns)
            {
                if (string.IsNullOrWhiteSpace(col.Key))
                     return Result.Fail(new RecipeError($"In ActionsDefs.yaml, action {action.Id} ('{action.Name}') has a column with an empty key.", RecipeErrorCodes.ConfigInvalidSchema));
                
                if (!uniqueColumnKeys.Add(col.Key))
                    return Result.Fail(new RecipeError($"In ActionsDefs.yaml, action {action.Id} ('{action.Name}') has a duplicate column key '{col.Key}'.", RecipeErrorCodes.ConfigInvalidSchema));

                if (!allColumnKeys.Contains(col.Key))
                {
                    return Result.Fail(new RecipeError(
                        $"In ActionsDefs.yaml, action {action.Id} ('{action.Name}') refers to an unknown column key '{col.Key}'.",
                        RecipeErrorCodes.ConfigMissingReference));
                }

                if (string.Equals(col.PropertyTypeId, "Enum", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(col.GroupName))
                {
                    return Result.Fail(new RecipeError(
                        $"In ActionsDefs.yaml, action {action.Id} ('{action.Name}'): column '{col.Key}' is of type 'Enum' but is missing a 'GroupName'.", 
                        RecipeErrorCodes.ConfigInvalidSchema));
                }
            }
        }

        return Result.Ok();
    }
}