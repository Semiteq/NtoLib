#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Steps;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv;

/// <summary>
/// A utility class responsible for mapping step records between their
/// CSV representation and the domain model. This class adheres to a
/// strategy defined by actions, where the applicable fields are determined
/// by the action set. It ensures safe reading, validation of extra fields,
/// and step construction.
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
    /// A tuple containing the mapped <see cref="Step"/> object (or null if mapping fails) and a list of errors encountered during mapping.
    /// </returns>
    public (Step? Step, IImmutableList<RecipeFileError> Errors) FromRecord(
        int lineNumber,
        string[] record,
        CsvHeaderBinder.Binding binding)
    {
        var errors = ImmutableList.CreateBuilder<RecipeFileError>();

        // actionId if mandatory
        var actionIdx = FindFileIndex(binding, ColumnKey.Action);
        if (actionIdx < 0 || actionIdx >= record.Length)
            return (null, errors.ToImmutable().Add(new RecipeFileError(lineNumber, null, "Missing 'Action' column")));

        if (!int.TryParse(record[actionIdx], NumberStyles.Integer, CultureInfo.InvariantCulture, out var actionId))
            return (null, errors.ToImmutable().Add(new RecipeFileError(lineNumber, ColumnKey.Action, $"Invalid action id '{record[actionIdx]}'")));

        // builder with defaults
        StepBuilder builder;
        try
        {
            builder = _stepFactory.ForAction(actionId);
        }
        catch (Exception ex)
        {
            return (null, errors.ToImmutable().Add(new RecipeFileError(lineNumber, ColumnKey.Action, $"Unknown action id '{actionId}'", ex)));
        }
        
        // todo: bind to action properties
        // parsing and applying properteis
        var target = TryParseInt(binding, record, ColumnKey.ActionTarget, errors, lineNumber);
        var initialValue = TryParseFloat(binding, record, ColumnKey.InitialValue, errors, lineNumber);
        var setpoint = TryParseFloat(binding, record, ColumnKey.Setpoint, errors, lineNumber);
        var speed = TryParseFloat(binding, record, ColumnKey.Speed, errors, lineNumber);
        var duration = TryParseFloat(binding, record, ColumnKey.StepDuration, errors, lineNumber);
        var comment = TryGetRaw(binding, record, ColumnKey.Comment);

        if (errors.Count > 0) return (null, errors.ToImmutable());

        builder = builder
            .WithOptionalTarget(target)
            .WithOptionalInitialValue(initialValue)
            .WithOptionalSetpoint(setpoint)
            .WithOptionalSpeed(speed)
            .WithOptionalDuration(duration)
            .WithOptionalComment(comment)
            .WithDeployDuration(_actionManager.GetActionEntryById(actionId).DeployDuration);

        // validation
        var supported = builder.NonNullKeys.ToHashSet();
        foreach (var (i, value) in EnumerateIndexValues(record))
        {
            var key = binding.FileIndexToColumn[i].Key;
            if (key is ColumnKey.Action or ColumnKey.StepStartTime) continue;
            if (!supported.Contains(key) && !string.IsNullOrWhiteSpace(value))
            {
                return (null, errors.ToImmutable().Add(new RecipeFileError(
                    lineNumber, key, $"Column '{key}' is not applicable for action {actionId} but has value '{value}'")));
            }
        }

        var step = builder.Build();
        return (step, errors.ToImmutable());
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
                result[i] = string.Empty; continue; 
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
            if (kv.Value.Key == key) return kv.Key;
        return -1;
    }
    
    private int? TryParseInt(CsvHeaderBinder.Binding binding, string[] record, ColumnKey key,
        ImmutableList<RecipeFileError>.Builder errors, int lineNumber)
    {
        var idx = FindFileIndex(binding, key);
        
        if (idx < 0 || idx >= record.Length) 
            return null;
        
        var raw = record[idx];
        
        if (string.IsNullOrWhiteSpace(raw)) 
            return null;

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            return parsedValue;
        
        errors.Add(new RecipeFileError(lineNumber, key, $"Invalid int '{raw}'"));
        return null;
    }
    
    private float? TryParseFloat(CsvHeaderBinder.Binding binding, string[] record, ColumnKey key, 
        ImmutableList<RecipeFileError>.Builder errors, int lineNumber)
    {
        var idx = FindFileIndex(binding, key);
        
        if (idx < 0 || idx >= record.Length) 
            return null;
        
        var raw = record[idx];
        
        if (string.IsNullOrWhiteSpace(raw)) 
            return null;

        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
            return parsedValue;
        
        
        errors.Add(new RecipeFileError(lineNumber, key, $"Invalid float '{raw}'"));
        return null;
        
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