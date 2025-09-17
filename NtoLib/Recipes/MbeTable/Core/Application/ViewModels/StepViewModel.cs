#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;

namespace NtoLib.Recipes.MbeTable.Core.Application.ViewModels;

/// <summary>
/// A pure data adapter for a single step. Its properties are accessed dynamically
/// via a custom PropertyDescriptor, driven by the ITypedList implementation on its parent collection.
/// Implements INotifyPropertyChanged to allow for efficient UI updates.
/// </summary>
public sealed class StepViewModel : INotifyPropertyChanged
{
    private Step _stepRecord;
    private readonly Action<int, ColumnIdentifier, object> _updatePropertyAction;
    private readonly Func<ColumnIdentifier, List<KeyValuePair<int, string>>> _enumOptionsProvider;
    private readonly ILogger _debugLogger;
    private float _stepStartTimeSeconds;
    private int _rowIndex;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="StepViewModel"/> class.
    /// </summary>
    /// <param name="stepRecord">The underlying domain model for the step.</param>
    /// <param name="rowIndex">The zero-based index of this step in the recipe.</param>
    /// <param name="updatePropertyAction">Callback action to invoke when a property is changed by the user.</param>
    /// <param name="startTime">The calculated start time for this step.</param>
    /// <param name="enumOptionsProvider">A function that provides enum options for a given column.</param>
    /// <param name="debugLogger">The logger instance for debugging purposes.</param>
    public StepViewModel(
        Step stepRecord,
        int rowIndex,
        Action<int, ColumnIdentifier, object> updatePropertyAction,
        TimeSpan startTime,
        Func<ColumnIdentifier, List<KeyValuePair<int, string>>> enumOptionsProvider,
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
    /// Gets a property value for data binding.
    /// </summary>
    /// <param name="identifier">The column identifier for the property.</param>
    /// <returns>The property value, formatted for display.</returns>
    public object? GetPropertyValue(ColumnIdentifier identifier)
    {
        try
        {
            if (identifier == WellKnownColumns.StepStartTime)
            {
                return StepStartTime;
            }

            if (!_stepRecord.Properties.TryGetValue(identifier, out var property) || property == null)
            {
                return null;
            }

            // For Action, the binding expects the raw ID (int). For others, the formatted string.
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
    /// Sets a property value from a data binding update.
    /// </summary>
    /// <param name="identifier">The column identifier for the property.</param>
    /// <param name="value">The new value from the UI.</param>
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
    /// <param name="identifier">The column identifier.</param>
    /// <returns>The <see cref="StepProperty"/> instance or null if not found.</returns>
    public StepProperty? GetProperty(ColumnIdentifier identifier)
    {
        _stepRecord.Properties.TryGetValue(identifier, out var property);
        return property;
    }

    /// <summary>
    /// Updates the ViewModel's internal state from a new domain model and notifies the UI.
    /// </summary>
    /// <param name="newStepRecord">The new step data from the domain.</param>
    /// <param name="newRowIndex">The new index of this view model in the list.</param>
    /// <param name="newStartTime">The new calculated start time.</param>
    internal void UpdateInPlace(Step newStepRecord, int newRowIndex, TimeSpan newStartTime)
    {
        _stepRecord = newStepRecord;
        _rowIndex = newRowIndex;
        
        var newStartTimeSeconds = (float)newStartTime.TotalSeconds;
        bool timeChanged = Math.Abs(_stepStartTimeSeconds - newStartTimeSeconds) > 1e-6f;
        _stepStartTimeSeconds = newStartTimeSeconds;
        
        // A full update is required, so we notify that all properties might have changed.
        // If only time changed, we could optimize by notifying only StepStartTime.
        OnPropertyChanged(string.Empty);
    }
    
    /// <summary>
    /// Retrieves combobox items (ID to display name) for a specific column.
    /// </summary>
    /// <param name="key">The column key.</param>
    /// <returns>A list of key-value pairs for the combobox.</returns>
    public List<KeyValuePair<int, string>> GetComboItems(ColumnIdentifier key)
    {
        try
        {
            return _enumOptionsProvider(key);
        }
        catch (Exception ex)
        {
            _debugLogger.LogException(ex, $"Error getting combo items for key '{key.Value}'");
            return new List<KeyValuePair<int, string>>();
        }
    }
    
    /// <summary>
    /// Determines if a property/cell should be disabled.
    /// </summary>
    /// <param name="key">The column key.</param>
    /// <returns>True if the property is not applicable for the current step's action.</returns>
    public bool IsPropertyDisabled(ColumnIdentifier key)
    {
        return !_stepRecord.Properties.TryGetValue(key, out var property) || property == null;
    }

    /// <summary>
    /// Determines if a property/cell is programmatically read-only.
    /// </summary>
    /// <param name="key">The column key.</param>
    /// <returns>True if the property is read-only.</returns>
    public bool IsPropertyReadonly(ColumnIdentifier key) => key == WellKnownColumns.StepStartTime;

    private string FormatTime(float seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);
        return $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}