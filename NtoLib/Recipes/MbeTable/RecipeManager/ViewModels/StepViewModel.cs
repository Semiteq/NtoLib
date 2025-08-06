#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NtoLib.Recipes.MbeTable.RecipeManager.StepManager;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.RecipeManager.ViewModels
{
    /// <summary>
    /// A pure data adapter for a single step, providing calculated data for the UI.
    /// It delegates all update logic to its owner (RecipeViewModel).
    /// </summary>
    public sealed class StepViewModel : INotifyPropertyChanged
    {
        private readonly Step _stepRecord;
        private readonly TableSchema _tableSchema;
        private readonly Action<ColumnKey, object> _updatePropertyAction;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// The calculated nesting level for UI indentation.
        /// </summary>
        public int NestingLevel { get; }

        /// <summary>
        /// The calculated start time, formatted for display.
        /// </summary>
        public string StartTimeDisplay { get; }

        public IReadOnlyDictionary<int, string> AvailableActions { get; }
        public IReadOnlyDictionary<int, string> AvailableActionTargets { get; }

        public StepViewModel(
            Step stepRecord,
            TableSchema tableSchema,
            Action<ColumnKey, object> updatePropertyAction,
            int nestingLevel,
            TimeSpan startTime,
            IReadOnlyDictionary<int, string> availableActions,
            IReadOnlyDictionary<int, string>? availableActionTargets)
        {
            _stepRecord = stepRecord;
            _tableSchema = tableSchema;
            _updatePropertyAction = updatePropertyAction;

            NestingLevel = nestingLevel;
            StartTimeDisplay = startTime.ToString(@"hh\:mm\:ss");

            AvailableActions = availableActions;
            AvailableActionTargets = availableActionTargets ?? new Dictionary<int, string>();
        }

        public object? this[ColumnKey key]
        {
            get
            {
                var property = _stepRecord.Properties[key];
                return property != null ? property.GetValueAsObject() : GetDefaultValueForType(key);
            }
            set
            {
                if (value is null) return;
                _updatePropertyAction(key, value);
            }
        }

        public bool IsPropertyAvailable(ColumnKey key) => _stepRecord.Properties[key] != null;

        public void RaisePropertyChanged(ColumnKey key) => OnPropertyChanged($"Item[{key}]");

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private object GetDefaultValueForType(ColumnKey key)
        {
            var systemType = _tableSchema.GetColumnDefinition(key).Type;
            if (systemType.IsValueType)
                return Activator.CreateInstance(systemType) ?? default;
            if (systemType == typeof(string))
                return string.Empty;
            return null!;
        }
    }
}