#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config;
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
        // ActionId is mandatory.
        var actionIdx = FindFileIndex(binding, WellKnownColumns.Action);
        if (actionIdx < 0 || actionIdx >= record.Length)
            return Result.Fail(new RecipeError("Missing 'Action' column", lineNumber));

        if (!int.TryParse(record[actionIdx], NumberStyles.Integer, CultureInfo.InvariantCulture, out var actionId))
            return Result.Fail(new RecipeError($"Invalid action id '{record[actionIdx]}'", lineNumber));

        // Builder with defaults.
        IStepBuilder builder;
        try
        {
            builder = _stepFactory.ForAction(actionId);
        }
        catch (Exception ex)
        {
            return Result.Fail(new RecipeError($"Unknown action id '{actionId}'", lineNumber)
                .CausedBy(ex));
        }
        
        // Parsing and applying properties.
        var targetResult = TryParseInt(binding, record, WellKnownColumns.ActionTarget, lineNumber);
        if (targetResult.IsFailed) return targetResult.ToResult();
        
        var initialValueResult = TryParseFloat(binding, record, WellKnownColumns.InitialValue, lineNumber);
        if (initialValueResult.IsFailed) return initialValueResult.ToResult();

        var setpointResult = TryParseFloat(binding, record, WellKnownColumns.Setpoint, lineNumber);
        if (setpointResult.IsFailed) return setpointResult.ToResult();
        
        var speedResult = TryParseFloat(binding, record, WellKnownColumns.Speed, lineNumber);
        if (speedResult.IsFailed) return speedResult.ToResult();

        var durationResult = TryParseFloat(binding, record, WellKnownColumns.StepDuration, lineNumber);
        if (durationResult.IsFailed) return durationResult.ToResult();
        
        var comment = TryGetRaw(binding, record, WellKnownColumns.Comment);

        builder = builder
            .WithOptionalTarget(targetResult.Value)
            .WithOptionalInitialValue(initialValueResult.Value)
            .WithOptionalSetpoint(setpointResult.Value)
            .WithOptionalSpeed(speedResult.Value)
            .WithOptionalDuration(durationResult.Value)
            .WithOptionalComment(comment);
        
        // Validation of extra fields.
        var supported = builder.NonNullKeys.ToHashSet();
        foreach (var (i, value) in EnumerateIndexValues(record))
        {
            var key = binding.FileIndexToColumn[i].Key;
            if (key == WellKnownColumns.Action || key == WellKnownColumns.StepStartTime) continue;
            
            if (!supported.Contains(key) && !string.IsNullOrWhiteSpace(value))
            {
                return Result.Fail(new RecipeError(
                    $"Column '{key.Value}' is not applicable for action {actionId} but has value '{value}'",
                    lineNumber));
            }
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
    
    private int FindFileIndex(CsvHeaderBinder.Binding binding, ColumnIdentifier key)
    {
        foreach (var kv in binding.FileIndexToColumn)
        {
            if (kv.Value.Key == key) return kv.Key;
        }
        return -1;
    }
    
    private Result<int?> TryParseInt(CsvHeaderBinder.Binding binding, string[] record, ColumnIdentifier key, int lineNumber)
    {
        var idx = FindFileIndex(binding, key);
        if (idx < 0 || idx >= record.Length) 
            return Result.Ok<int?>(null);
        
        var raw = record[idx];
        if (string.IsNullOrWhiteSpace(raw)) 
            return Result.Ok<int?>(null);

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            return Result.Ok<int?>(parsedValue);
        
        return Result.Fail(new RecipeError($"Invalid int '{raw}' in column '{key.Value}'", lineNumber));
    }
    
    private Result<float?> TryParseFloat(CsvHeaderBinder.Binding binding, string[] record, ColumnIdentifier key, int lineNumber)
    {
        var idx = FindFileIndex(binding, key);
        if (idx < 0 || idx >= record.Length) 
            return Result.Ok<float?>(null);
        
        var raw = record[idx];
        if (string.IsNullOrWhiteSpace(raw)) 
            return Result.Ok<float?>(null);

        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
            return Result.Ok<float?>(parsedValue);
        
        return Result.Fail(new RecipeError($"Invalid float '{raw}' in column '{key.Value}'", lineNumber));
    }
    
    private string? TryGetRaw(CsvHeaderBinder.Binding binding, string[] record, ColumnIdentifier key)
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