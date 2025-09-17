#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Errors;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Validators;

/// <summary>
/// Validates column definitions from configuration.
/// </summary>
public sealed class ColumnDefsValidator : IColumnDefsValidator
{
    /// <inheritdoc />
    public Result Validate(IReadOnlyCollection<YamlColumnDefinition> definitions, PropertyDefinitionRegistry registry)
    {
        if (definitions == null || definitions.Count == 0)
            return Result.Fail(new RecipeError("ColumnDefs.yaml is empty or invalid.", RecipeErrorCodes.ConfigInvalidSchema));

        var keySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (dto, order) in definitions.Select((d, i) => (d, i)))
        {
            if (string.IsNullOrWhiteSpace(dto.Key))
                return Result.Fail(new RecipeError($"In ColumnDefs.yaml, column at index {order} has empty key.", RecipeErrorCodes.ConfigInvalidSchema));

            if (!keySet.Add(dto.Key))
                return Result.Fail(new RecipeError($"In ColumnDefs.yaml, duplicate column key '{dto.Key}'.", RecipeErrorCodes.ConfigInvalidSchema));

            if (dto.BusinessLogic == null)
                return Result.Fail(new RecipeError($"In ColumnDefs.yaml, column '{dto.Key}' missing business-logic section.", RecipeErrorCodes.ConfigInvalidSchema));

            if (dto.Ui == null)
                return Result.Fail(new RecipeError($"In ColumnDefs.yaml, column '{dto.Key}' missing ui section.", RecipeErrorCodes.ConfigInvalidSchema));

            if (string.IsNullOrWhiteSpace(dto.BusinessLogic.PropertyTypeId))
                return Result.Fail(new RecipeError($"In ColumnDefs.yaml, column '{dto.Key}' missing property-type-id.", RecipeErrorCodes.ConfigInvalidSchema));

            try
            {
                // Validate that the property type exists in the registry.
                _ = registry.GetDefinition(dto.BusinessLogic.PropertyTypeId);
            }
            catch (KeyNotFoundException ex)
            {
                return Result.Fail(new RecipeError($"Error in ColumnDefs.yaml: {ex.Message}", RecipeErrorCodes.ConfigMissingReference).CausedBy(ex));
            }

            if (dto.BusinessLogic.Calculation != null)
            {
                if (string.IsNullOrWhiteSpace(dto.BusinessLogic.Calculation.Formula))
                    return Result.Fail(new RecipeError($"In ColumnDefs.yaml, column '{dto.Key}' has an empty formula.", RecipeErrorCodes.ConfigInvalidSchema));
            }
        }
        
        var allKeys = definitions.Select(d => d.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var c in definitions.Where(c => c.BusinessLogic?.Calculation != null))
        {
            var formula = c.BusinessLogic!.Calculation!.Formula ?? string.Empty;
            var deps = GetDependenciesSimple(formula); 
            
            if (deps.Any(d => d.Equals(c.Key, StringComparison.OrdinalIgnoreCase)))
                return Result.Fail(new RecipeError($"In ColumnDefs.yaml, column '{c.Key}' formula directly references itself.", RecipeErrorCodes.ConfigInvalidSchema));
                
            foreach (var dep in deps)
            {
                if (!allKeys.Contains(dep))
                    return Result.Fail(new RecipeError(
                        $"In ColumnDefs.yaml, column '{c.Key}' formula depends on unknown column '{dep}'.",
                        RecipeErrorCodes.ConfigInvalidSchema));
            }
        }
        
        return Result.Ok();
    }

    private IEnumerable<string> GetDependenciesSimple(string formula)
    {
        return Regex.Matches(formula, @"\[([a-zA-Z0-9_-]+)\]")
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}