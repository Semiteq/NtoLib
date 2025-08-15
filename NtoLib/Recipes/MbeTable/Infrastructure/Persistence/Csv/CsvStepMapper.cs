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
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv;

/// <summary>
/// A utility class responsible for mapping step records between their
/// CSV representation and the domain model. This class adheres to a
/// strategy defined by actions, where the applicable fields are determined
/// by the action set. It ensures safe reading, validation of extra fields,
/// and step construction.
/// </summary>
public sealed class CsvStepMapper
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
            builder = _stepFactory.For(actionId);
        }
        catch (Exception ex)
        {
            return (null, errors.ToImmutable().Add(new RecipeFileError(lineNumber, ColumnKey.Action, $"Unknown action id '{actionId}'", ex)));
        }

        // parsing and applying properteis
        TryParseInt(binding, record, ColumnKey.ActionTarget, out var target, errors, lineNumber);
        TryParseFloat(binding, record, ColumnKey.InitialValue, out var initialValue, errors, lineNumber);
        TryParseFloat(binding, record, ColumnKey.Setpoint, out var setpoint, errors, lineNumber);
        TryParseFloat(binding, record, ColumnKey.Speed, out var speed, errors, lineNumber);
        TryParseFloat(binding, record, ColumnKey.StepDuration, out var duration, errors, lineNumber);
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
        for (int i = 0; i < orderedColumns.Count; i++)
        {
            var col = orderedColumns[i];
            if (col.Key == ColumnKey.StepStartTime) { result[i] = string.Empty; continue; }

            if (!step.Properties.TryGetValue(col.Key, out var prop) || prop is null)
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
    /// Retrieves the file index for a given column key from the provided CSV binding.
    /// </summary>
    /// <param name="binding">The CSV header binding that maps file indices to column definitions.</param>
    /// <param name="key">The column key to locate within the binding.</param>
    /// <returns>
    /// The file index corresponding to the specified column key, or -1 if the column key is not found.
    /// </returns>
    private static int FindFileIndex(CsvHeaderBinder.Binding binding, ColumnKey key)
    {
        foreach (var kv in binding.FileIndexToColumn)
            if (kv.Value.Key == key) return kv.Key;
        return -1;
    }

    /// <summary>
    /// Parses an integer value from a specific column in a CSV record and validates it.
    /// Adds an error to the error list if parsing fails.
    /// </summary>
    /// <param name="binding">The header binding that maps column keys to file indices.</param>
    /// <param name="record">The CSV record as an array of strings.</param>
    /// <param name="key">The key identifying the column to parse in the CSV record.</param>
    /// <param name="value">The parsed integer value if successful, or null if parsing fails.</param>
    /// <param name="errors">A collection where errors encountered during parsing are added.</param>
    /// <param name="lineNumber">The line number of the CSV record, used for error reporting.</param>
    private static void TryParseInt(CsvHeaderBinder.Binding binding, string[] record, ColumnKey key,
        out int? value, ImmutableList<RecipeFileError>.Builder errors, int lineNumber)
    {
        value = null;
        var idx = FindFileIndex(binding, key);
        if (idx < 0 || idx >= record.Length) return;
        var raw = record[idx];
        if (string.IsNullOrWhiteSpace(raw)) return;

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) value = v;
        else errors.Add(new RecipeFileError(lineNumber, key, $"Invalid int '{raw}'"));
    }

    /// <summary>
    /// Attempts to parse a floating-point value from a CSV record and handles errors if parsing fails.
    /// </summary>
    /// <param name="binding">The header binding that maps column keys to record indices.</param>
    /// <param name="record">The CSV record as an array of strings.</param>
    /// <param name="key">The column key identifying the data to parse from the record.</param>
    /// <param name="value">
    /// The result of the parse operation as a nullable float. Set to null if the value is not present or cannot be parsed.
    /// </param>
    /// <param name="errors">
    /// A list builder for collecting errors encountered during parsing, such as invalid or missing float values.
    /// </param>
    /// <param name="lineNumber">The line number of the CSV record, used for error tracking.</param>
    private static void TryParseFloat(CsvHeaderBinder.Binding binding, string[] record, ColumnKey key,
        out float? value, ImmutableList<RecipeFileError>.Builder errors, int lineNumber)
    {
        value = null;
        var idx = FindFileIndex(binding, key);
        if (idx < 0 || idx >= record.Length) return;
        var raw = record[idx];
        if (string.IsNullOrWhiteSpace(raw)) return;

        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) value = v;
        else errors.Add(new RecipeFileError(lineNumber, key, $"Invalid float '{raw}'"));
    }

    /// <summary>
    /// Retrieves the raw value from a record based on the specified column key by locating its corresponding index in the binding.
    /// </summary>
    /// <param name="binding">The binding that maps column keys to their indices in the record.</param>
    /// <param name="record">The CSV record represented as an array of strings.</param>
    /// <param name="key">The column key to locate within the binding to retrieve its value from the record.</param>
    /// <returns>
    /// The raw value from the record corresponding to the specified column key, or null if the key is not found or the index is out of range.
    /// </returns>
    private static string? TryGetRaw(CsvHeaderBinder.Binding binding, string[] record, ColumnKey key)
    {
        var idx = FindFileIndex(binding, key);
        if (idx < 0 || idx >= record.Length) return null;
        return record[idx];
    }

    /// <summary>
    /// Enumerates the index and corresponding string value of each element in a given array.
    /// </summary>
    /// <param name="record">An array of strings representing the input data to be enumerated.</param>
    /// <returns>
    /// An enumerable collection of tuples where each tuple contains the index and corresponding value
    /// of an element in the input array.
    /// </returns>
    private static IEnumerable<(int Index, string Value)> EnumerateIndexValues(string[] record)
    {
        for (int i = 0; i < record.Length; i++)
            yield return (i, record[i]);
    }
}