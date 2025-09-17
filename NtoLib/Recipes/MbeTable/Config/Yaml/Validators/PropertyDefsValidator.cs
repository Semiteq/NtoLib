#nullable enable

using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Properties;
using NtoLib.Recipes.MbeTable.Errors;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Validators;

/// <summary>
/// Validates property definitions from configuration.
/// </summary>
public sealed class PropertyDefsValidator : IPropertyDefsValidator
{
    private static readonly HashSet<string> SupportedSystemTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "System.String",
        "System.Int32",
        "System.Single",
    };
    
    // Special types handled by dedicated logic
    private static readonly HashSet<string> SupportedSpecialTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Time",
        "Enum"
    };

    /// <inheritdoc />
    public Result Validate(IReadOnlyCollection<YamlPropertyDefinition> definitions)
    {
        if (definitions == null || definitions.Count == 0)
            return Result.Fail(new RecipeError("PropertyDefinitions.yaml is empty or invalid.", RecipeErrorCodes.ConfigInvalidSchema));

        var uniqueIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in definitions)
        {
            if (string.IsNullOrWhiteSpace(def.PropertyTypeId))
                return Result.Fail(new RecipeError("In PropertyDefinitions.yaml, a property definition is missing its 'PropertyTypeId'.", RecipeErrorCodes.ConfigInvalidSchema));

            if (!uniqueIds.Add(def.PropertyTypeId))
                return Result.Fail(new RecipeError($"In PropertyDefinitions.yaml, duplicate PropertyTypeId detected: '{def.PropertyTypeId}'.", RecipeErrorCodes.ConfigInvalidSchema));

            if (string.IsNullOrWhiteSpace(def.SystemType))
                return Result.Fail(new RecipeError($"In PropertyDefinitions.yaml, SystemType is required for '{def.PropertyTypeId}'.", RecipeErrorCodes.ConfigInvalidSchema));

            var typeCheckResult = IsSupportedType(def);
            if (typeCheckResult.IsFailed)
                return typeCheckResult;
        }

        return Result.Ok();
    }
    
    private static Result IsSupportedType(YamlPropertyDefinition def)
    {
        // Check for special types first
        if (SupportedSpecialTypes.Contains(def.PropertyTypeId))
            return Result.Ok();

        var sysType = Type.GetType(def.SystemType, throwOnError: false, ignoreCase: true);
        if (sysType == null)
        {
            return Result.Fail(new RecipeError($"In PropertyDefinitions.yaml, unknown SystemType '{def.SystemType}' for '{def.PropertyTypeId}'.", RecipeErrorCodes.ConfigInvalidSchema));
        }

        if (SupportedSystemTypes.Contains(sysType.FullName))
            return Result.Ok();
        
        return Result.Fail(new RecipeError(
            $"In PropertyDefinitions.yaml, unsupported SystemType '{def.SystemType}' for PropertyTypeId '{def.PropertyTypeId}'.",
            RecipeErrorCodes.ConfigInvalidSchema));
    }
}