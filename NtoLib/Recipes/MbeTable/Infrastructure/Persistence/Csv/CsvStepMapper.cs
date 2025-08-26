#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
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
    private readonly StepFactory _stepFactory;
    private readonly ActionManager _actionManager;

    public CsvStepMapper(StepFactory stepFactory, ActionManager actionManager)
    {
        _stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
        _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
    }

    /// <summary>
    /// Maps a CSV record to a <see cref="Step"/> object while validating and handling errors.
    /// </summary>
    /// <param name="lineNumber">The line number of the CSV record, used for error tracking.</param>
    /// <param name="record">The CSV record as an array of strings.</param>
    /// <param name="binding">The header binding that maps column keys to record indices.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the mapped <see cref="Step"/> object on success, or an error on failure.
    /// </returns>
    public Result<Step> FromRecord(
        int lineNumber,
        string[] record,
        CsvHeaderBinder.Binding binding)
    {
        // ActionId is mandatory.
        var actionIdx = FindFileIndex(binding, ColumnKey.Action);
        if (actionIdx < 0 || actionIdx >= record.Length)
            return Result.Fail(new RecipeError("Missing 'Action' column", lineNumber));

        if (!int.TryParse(record[actionIdx], NumberStyles.Integer, CultureInfo.InvariantCulture, out var actionId))
            return Result.Fail(new RecipeError($"Invalid action id '{record[actionIdx]}'", lineNumber, ColumnKey.Action));

        // Builder with defaults.
        StepBuilder builder;
        try
        {
            builder = _stepFactory.ForAction(actionId);
        }
        catch (Exception ex)
        {
            return Result.Fail(new RecipeError($"Unknown action id '{actionId}'", lineNumber, ColumnKey.Action)
                .CausedBy(ex));
        }
        
        // Parsing and applying properties.
        var targetResult = TryParseInt(binding, record, ColumnKey.ActionTarget, lineNumber);
        if (targetResult.IsFailed) return targetResult.ToResult();
        
        var initialValueResult = TryParseFloat(binding, record, ColumnKey.InitialValue, lineNumber);
        if (initialValueResult.IsFailed) return initialValueResult.ToResult();

        var setpointResult = TryParseFloat(binding, record, ColumnKey.Setpoint, lineNumber);
        if (setpointResult.IsFailed) return setpointResult.ToResult();
        
        var speedResult = TryParseFloat(binding, record, ColumnKey.Speed, lineNumber);
        if (speedResult.IsFailed) return speedResult.ToResult();

        var durationResult = TryParseFloat(binding, record, ColumnKey.StepDuration, lineNumber);
        if (durationResult.IsFailed) return durationResult.ToResult();
        
        var comment = TryGetRaw(binding, record, ColumnKey.Comment);

        builder = builder
            .WithOptionalTarget(targetResult.Value)
            .WithOptionalInitialValue(initialValueResult.Value)
            .WithOptionalSetpoint(setpointResult.Value)
            .WithOptionalSpeed(speedResult.Value)
            .WithOptionalDuration(durationResult.Value)
            .WithOptionalComment(comment)
            .WithDeployDuration(_actionManager.GetActionEntryById(actionId).DeployDuration);

        // Validation of extra fields.
        var supported = builder.NonNullKeys.ToHashSet();
        foreach (var (i, value) in EnumerateIndexValues(record))
        {
            var key = binding.FileIndexToColumn[i].Key;
            if (key is ColumnKey.Action or ColumnKey.StepStartTime) continue;
            
            if (!supported.Contains(key) && !string.IsNullOrWhiteSpace(value))
            {
                return Result.Fail(new RecipeError(
                    $"Column '{key}' is not applicable for action {actionId} but has value '{value}'",
                    lineNumber, 
                    key));
            }
        }

        return Result.Ok(builder.Build());
    }

    /// <summary>
    /// Converts a <see cref="Step"/> object into a CSV record as an array of strings based on the given column order.
    /// </summary>
    /// <param name="step">The <see cref="Step"/> object containing properties to map to a CSV record.</param>
    /// <param name="orderedColumns">A list of <see cref="ColumnDefinition"/> objects defining the order and structure of the columns.</param>
    /// <returns>
    /// An array of strings representing the CSV record corresponding to the <see cref="Step"/> object, with values mapped according to the column definitions.
    /// </returns>
    public string[] ToRecord(Step step, IReadOnlyList<ColumnDefinition> orderedColumns)
    {
        var result = new string[orderedColumns.Count];
        for (var i = 0; i < orderedColumns.Count; i++)
        {
            var col = orderedColumns[i];
            if (col.Key == ColumnKey.StepStartTime || !step.Properties.TryGetValue(col.Key, out var prop) ||
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
    
    private int FindFileIndex(CsvHeaderBinder.Binding binding, ColumnKey key)
    {
        foreach (var kv in binding.FileIndexToColumn)
        {
            if (kv.Value.Key == key) return kv.Key;
        }
        return -1;
    }
    
    private Result<int?> TryParseInt(CsvHeaderBinder.Binding binding, string[] record, ColumnKey key, int lineNumber)
    {
        var idx = FindFileIndex(binding, key);
        if (idx < 0 || idx >= record.Length) 
            return Result.Ok<int?>(null);
        
        var raw = record[idx];
        if (string.IsNullOrWhiteSpace(raw)) 
            return Result.Ok<int?>(null);

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            return Result.Ok<int?>(parsedValue);
        
        return Result.Fail(new RecipeError($"Invalid int '{raw}'", lineNumber, key));
    }
    
    private Result<float?> TryParseFloat(CsvHeaderBinder.Binding binding, string[] record, ColumnKey key, int lineNumber)
    {
        var idx = FindFileIndex(binding, key);
        if (idx < 0 || idx >= record.Length) 
            return Result.Ok<float?>(null);
        
        var raw = record[idx];
        if (string.IsNullOrWhiteSpace(raw)) 
            return Result.Ok<float?>(null);

        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
            return Result.Ok<float?>(parsedValue);
        
        return Result.Fail(new RecipeError($"Invalid float '{raw}'", lineNumber, key));
    }
    
    private string? TryGetRaw(CsvHeaderBinder.Binding binding, string[] record, ColumnKey key)
    {
        var idx = FindFileIndex(binding, key);
        if (idx < 0 || idx >= record.Length) return null;
        return record[idx];
    }

    private IEnumerable<(int Index, string Value)> EnumerateIndexValues(string[] record)
    {
        return record.Select((t, i) => (i, t));
    }
}