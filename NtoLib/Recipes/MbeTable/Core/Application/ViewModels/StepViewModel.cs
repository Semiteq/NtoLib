#nullable enable

using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Presentation.Table.State;

namespace NtoLib.Recipes.MbeTable.Core.Application.ViewModels;

/// <summary>
/// Pure data adapter for a single step. Accessed via VirtualMode queries.
/// No automatic change notifications - updates are managed by RecipeViewModel.
/// </summary>
public sealed class StepViewModel
{
    private Step _stepRecord;
    private readonly Action<int, ColumnIdentifier, object> _updatePropertyAction;
    private readonly Func<int, ColumnIdentifier, List<KeyValuePair<int, string>>> _enumOptionsProvider;
    private readonly ILogger _debugLogger;
    private float _stepStartTimeSeconds;
    private int _rowIndex;

    public StepViewModel(
        Step stepRecord,
        int rowIndex,
        Action<int, ColumnIdentifier, object> updatePropertyAction,
        TimeSpan startTime,
        Func<int, ColumnIdentifier, List<KeyValuePair<int, string>>> enumOptionsProvider,
        ILogger debugLogger)
    {
        _stepRecord = stepRecord ?? throw new ArgumentNullException(nameof(stepRecord));
        _updatePropertyAction = updatePropertyAction ?? throw new ArgumentNullException(nameof(updatePropertyAction));
        _enumOptionsProvider = enumOptionsProvider ?? throw new ArgumentNullException(nameof(enumOptionsProvider));
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
        _stepStartTimeSeconds = (float)startTime.TotalSeconds;
        _rowIndex = rowIndex;
    }

    /// <summary>
    /// Gets the formatted start time of the step.
    /// </summary>
    public string StepStartTime => FormatTime(_stepStartTimeSeconds);

    /// <summary>
    /// Gets the current action ID for this step.
    /// </summary>
    public int CurrentActionId
    {
        get
        {
            if (_stepRecord.Properties.TryGetValue(WellKnownColumns.Action, out var actionProp))
            {
                return actionProp?.GetValue<int>() ?? 0;
            }
            return 0;
        }
    }

    public object? GetPropertyValue(ColumnIdentifier identifier)
    {
        try
        {
            if (identifier == WellKnownColumns.StepStartTime)
            {
                var timeValue = StepStartTime;
                return timeValue;
            }

            if (!_stepRecord.Properties.TryGetValue(identifier, out var property) || property == null)
            {
                return null;
            }

            return identifier == WellKnownColumns.Action
                ? property.GetValueAsObject()
                : property.GetDisplayValue();
        }
        catch (Exception ex)
        {
            _debugLogger.LogException(ex, $"Error getting value for key '{identifier.Value}'");
            return null;
        }
    }

    /// <summary>
    /// Sets a property value from user input via editing control.
    /// </summary>
    public void SetPropertyValue(ColumnIdentifier identifier, object? value)
    {
        if (value == null) return;
        try
        {
            _updatePropertyAction(_rowIndex, identifier, value);
        }
        catch (Exception ex)
        {
            _debugLogger.LogException(ex, $"Error setting value for key '{identifier.Value}'");
        }
    }

    /// <summary>
    /// Retrieves the underlying StepProperty object.
    /// </summary>
    public StepProperty? GetProperty(ColumnIdentifier identifier)
    {
        _stepRecord.Properties.TryGetValue(identifier, out var property);
        return property;
    }

    /// <summary>
    /// Determines the data state of a cell based on property presence and configuration.
    /// </summary>
    /// <param name="key">Column identifier to check.</param>
    /// <returns>
    /// <see cref="CellDataState.ReadOnly"/> if column is step_start_time (calculated field).
    /// <see cref="CellDataState.Disabled"/> if property does not exist for current Action.
    /// <see cref="CellDataState.Normal"/> if property exists and is editable.
    /// </returns>
    public CellDataState GetDataState(ColumnIdentifier key)
    {
        // Special case: Step start time is always readonly (calculated field)
        if (key == WellKnownColumns.StepStartTime)
        {
            return CellDataState.ReadOnly;
        }

        // Check if property exists for current Action
        if (!_stepRecord.Properties.TryGetValue(key, out var property) || property == null)
        {
            return CellDataState.Disabled;
        }

        return CellDataState.Normal;
    }

    /// <summary>
    /// Updates the ViewModel's internal state from a new domain model.
    /// Called internally by RecipeViewModel after data changes.
    /// </summary>
    internal void UpdateInPlace(Step newStepRecord, int newRowIndex, TimeSpan newStartTime)
    {
        _stepRecord = newStepRecord;
        _rowIndex = newRowIndex;
        _stepStartTimeSeconds = (float)newStartTime.TotalSeconds;
    }

    /// <summary>
    /// Retrieves combobox items (ID to display name) for a specific column.
    /// Uses current actionId from _stepRecord, not captured at creation time.
    /// </summary>
    public List<KeyValuePair<int, string>> GetComboItems(ColumnIdentifier key)
    {
        try
        {
            return _enumOptionsProvider(CurrentActionId, key);
        }
        catch (Exception ex)
        {
            _debugLogger.LogException(ex, $"Error getting combo items for key '{key.Value}'");
            return new List<KeyValuePair<int, string>>();
        }
    }

    private string FormatTime(float seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);
        return $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";
    }
}