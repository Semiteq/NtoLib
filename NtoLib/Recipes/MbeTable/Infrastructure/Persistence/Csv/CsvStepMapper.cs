#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Steps;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv;

/// <summary>
/// A utility class responsible for mapping step records between their
/// CSV representation and the domain model.
/// </summary>
public sealed class CsvStepMapper : ICsvStepMapper
{
    private readonly IStepFactory _stepFactory;
    private readonly IActionRepository _actionRepository;

    public CsvStepMapper(IStepFactory stepFactory, IActionRepository actionRepository)
    {
        _stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
        _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
    }

    public Result<Step> FromRecord(
        int lineNumber,
        string[] record,
        CsvHeaderBinder.Binding binding)
    {
        var actionIdx = FindFileIndex(binding, WellKnownColumns.Action);
        if (actionIdx < 0 || actionIdx >= record.Length)
            return Result.Fail(new RecipeError("Missing 'Action' column in the recipe file", lineNumber));

        if (!int.TryParse(record[actionIdx], NumberStyles.Integer, CultureInfo.InvariantCulture, out var actionId))
            return Result.Fail(new RecipeError($"Invalid Action ID '{record[actionIdx]}'", lineNumber));

        IStepBuilder builder;
        try
        {
            builder = _stepFactory.ForAction(actionId);
        }
        catch (Exception ex)
        {
            return Result.Fail(new RecipeError($"Unknown Action ID '{actionId}'", lineNumber)
                .CausedBy(ex));
        }

        foreach (var kvp in binding.FileIndexToColumn)
        {
            var fileIndex = kvp.Key;
            var columnDef = kvp.Value;
            var columnKey = columnDef.Key;
            
            if (columnKey == WellKnownColumns.Action || columnKey == WellKnownColumns.StepStartTime)
                continue;
            
            var rawValue = (fileIndex < record.Length) ? record[fileIndex] : string.Empty;

            if (!builder.Supports(columnKey))
            {
                if (!string.IsNullOrWhiteSpace(rawValue))
                {
                    var actionName = _actionRepository.GetActionById(actionId).Name;
                    return Result.Fail(new RecipeError(
                        $"Column '{columnKey.Value}' is not applicable for action '{actionName}' but contains value '{rawValue}'",
                        lineNumber));
                }
                continue;
            }
            
            if (string.IsNullOrWhiteSpace(rawValue))
                continue;
            
            var parseResult = TryParseValue(rawValue, columnDef.SystemType);
            if (parseResult.IsFailed)
            {
                return Result.Fail(new RecipeError($"Invalid value '{rawValue}' in column '{columnKey.Value}'", lineNumber))
                    .WithErrors(parseResult.Errors);
            }
            
            builder.WithOptionalDynamic(columnKey, parseResult.Value);
        }

        return Result.Ok(builder.Build());
    }

    public string[] ToRecord(Step step, IReadOnlyList<ColumnDefinition> orderedColumns)
    {
        var result = new string[orderedColumns.Count];
        for (var i = 0; i < orderedColumns.Count; i++)
        {
            var col = orderedColumns[i];
            if (col.Key == WellKnownColumns.StepStartTime || !step.Properties.TryGetValue(col.Key, out var prop) ||
                prop is null)
            {
                result[i] = string.Empty;
                continue;
            }

            result[i] = prop.GetValueAsObject() switch
            {
                int ival => ival.ToString(CultureInfo.InvariantCulture),
                float fval => fval.ToString("R", CultureInfo.InvariantCulture),
                string s => s,
                _ => prop.GetValueAsObject()?.ToString() ?? string.Empty
            };
        }
        return result;
    }
    
    /// <summary>
    /// Parses a raw string value to the target type specified.
    /// </summary>
    private static Result<object> TryParseValue(string rawValue, Type targetType)
    {
        if (targetType == typeof(string))
        {
            return Result.Ok<object>(rawValue);
        }

        if (targetType == typeof(int))
        {
            return int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue)
                ? Result.Ok<object>(parsedValue)
                : Result.Fail("Value must be a valid integer.");
        }

        if (targetType == typeof(float))
        {
            return float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue)
                ? Result.Ok<object>(parsedValue)
                : Result.Fail("Value must be a valid floating-point number.");
        }
        
        return Result.Fail($"Unsupported type for CSV parsing: {targetType.Name}");
    }
    
    private int FindFileIndex(CsvHeaderBinder.Binding binding, ColumnIdentifier key)
    {
        // This helper can be optimized if needed, but is clear for now.
        return binding.FileIndexToColumn
            .FirstOrDefault(kv => kv.Value.Key == key)
            .Key;
    }
}