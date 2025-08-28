#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;

namespace NtoLib.Recipes.MbeTable.Core.Application.ViewModels
{
    /// <summary>
    /// A pure data adapter for a single step. Its properties are accessed dynamically
    /// via a custom PropertyDescriptor, driven by the ITypedList implementation on its parent collection.
    /// </summary>
    public sealed class StepViewModel : INotifyPropertyChanged
    {
        private readonly Step _stepRecord;
        private readonly Action<ColumnIdentifier, object> _updatePropertyAction;
        private readonly ILogger _debugLogger;
        private readonly float _stepStartTimeSeconds;

        public event PropertyChangedEventHandler? PropertyChanged;
        
        public List<KeyValuePair<int, string>> AvailableActionTargets { get; }

        public StepViewModel(
            Step stepRecord,
            Action<ColumnIdentifier, object> updatePropertyAction,
            TimeSpan startTime,
            List<KeyValuePair<int, string>>? availableActionTargets,
            ILogger debugLogger)
        {
            _stepRecord = stepRecord ?? throw new ArgumentNullException(nameof(stepRecord));
            _updatePropertyAction = updatePropertyAction ?? throw new ArgumentNullException(nameof(updatePropertyAction));
            _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
            _stepStartTimeSeconds = (float)startTime.TotalSeconds;
            AvailableActionTargets = availableActionTargets ?? new List<KeyValuePair<int, string>>();
        }

        // --- Public methods for data access, called by StepPropertyDescriptor ---
        
        public object? GetPropertyValue(ColumnIdentifier identifier)
        {
            try
            {
                if (identifier == WellKnownColumns.StepStartTime)
                {
                    return FormatTime(_stepStartTimeSeconds);
                }

                if (!_stepRecord.Properties.TryGetValue(identifier, out var property) || property == null)
                {
                    return null;
                }

                if (identifier == WellKnownColumns.Action || identifier == WellKnownColumns.ActionTarget)
                {
                    return property.GetValueAsObject();
                }

                return property.GetDisplayValue();
            }
            catch (Exception ex)
            {
                _debugLogger.LogException(ex, $"Error getting value for key '{identifier.Value}'");
                return null;
            }
        }

        public string? StepStartTime => FormatTime(_stepStartTimeSeconds);
        
        public void SetPropertyValue(ColumnIdentifier identifier, object? value)
        {
            if (value == null) return;
            try
            {
                _updatePropertyAction(identifier, value);
            }
            catch (Exception ex)
            {
                _debugLogger.LogException(ex, $"Error setting value for key '{identifier.Value}'");
            }
        }
        
        // --- Methods for styling ---
        
        
        /// <summary>
        /// Determines if a property/cell should be disabled (grayed out and non-editable).
        /// </summary>
        public bool IsPropertyDisabled(ColumnIdentifier key)
        {
            return !_stepRecord.Properties.TryGetValue(key, out var property) || property == null;
        }
        
        public bool IsPropertyReadonly(ColumnIdentifier key) => key == WellKnownColumns.StepStartTime;
        
        private string FormatTime(float seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);
            return $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }
    }
}