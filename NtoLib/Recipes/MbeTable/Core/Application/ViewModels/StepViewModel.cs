#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Application.ViewModels
{
    /// <summary>
    /// A pure data adapter for a single step, providing calculated data for the UI.
    /// It delegates all update logic to its owner (RecipeViewModel).
    /// </summary>
    public sealed class StepViewModel : INotifyPropertyChanged
    {
        private readonly Step _stepRecord;
        private readonly Action<ColumnKey, object> _updatePropertyAction;

        private readonly float _stepStartTimeSeconds;
        
        public event PropertyChangedEventHandler? PropertyChanged;

        public List<KeyValuePair<int, string>> AvailableActionTargets { get; }

        public StepViewModel(
            Step stepRecord,
            Action<ColumnKey, object> updatePropertyAction,
            TimeSpan startTime,
            List<KeyValuePair<int, string>>? availableActionTargets)
        {
            _stepRecord = stepRecord ?? throw new ArgumentNullException(nameof(stepRecord));
            _updatePropertyAction = updatePropertyAction ?? throw new ArgumentNullException(nameof(updatePropertyAction));

            _stepStartTimeSeconds = (float)startTime.TotalSeconds;

            AvailableActionTargets = availableActionTargets ?? new List<KeyValuePair<int, string>>();
        }

        public int? Action
        {
            get => _stepRecord.Properties[ColumnKey.Action]?.GetValue<int>();
            set
            {
                if (value == null || Action == value) return;
                _updatePropertyAction(ColumnKey.Action, value);
            }
        }

        public int? ActionTarget
        {
            get => _stepRecord.Properties[ColumnKey.ActionTarget]?.GetValue<int>();
            set
            {
                if (value == null || ActionTarget == value) return;
                _updatePropertyAction(ColumnKey.ActionTarget, value);
            }
        }

        public string? InitialValue
        {
            get => _stepRecord.Properties[ColumnKey.InitialValue]?.GetDisplayValue();
            set
            {
                if (value == null || InitialValue == value) return;
                _updatePropertyAction(ColumnKey.InitialValue, value);
            }
        }

        public string? Setpoint
        {
            get => _stepRecord.Properties[ColumnKey.Setpoint]?.GetDisplayValue();
            set
            {
                if (value == null || Setpoint == value) return;
                _updatePropertyAction(ColumnKey.Setpoint, value);
            }
        }

        public string? Speed
        {
            get => _stepRecord.Properties[ColumnKey.Speed]?.GetDisplayValue();
            set
            {
                if (value == null || Speed == value) return;
                _updatePropertyAction(ColumnKey.Speed, value);
            }
        }

        public string? StepDuration
        {
            get => _stepRecord.Properties[ColumnKey.StepDuration]?.GetDisplayValue();
            set
            {
                if (value == null || StepDuration == value) return;
                _updatePropertyAction(ColumnKey.StepDuration, value);
            }
        }

        public string? StepStartTime => FormatTime(_stepStartTimeSeconds);

        public string? Comment
        {
            get => _stepRecord.Properties[ColumnKey.Comment]?.GetDisplayValue();
            set
            {
                if (value == null || Comment == value) return;
                _updatePropertyAction(ColumnKey.Comment, value);
            }
        }
        
        public bool IsPropertyDisabled(ColumnKey key)
        {
            var propMissing = _stepRecord.Properties[key] == null;
            if (propMissing) return true;

            if (key == ColumnKey.ActionTarget)
            {
                if (AvailableActionTargets == null || AvailableActionTargets.Count == 0)
                    return true;

                if (!ActionTarget.HasValue)
                    return true;
            }
            return false;
        }
        
        public bool IsPropertyReadonly(ColumnKey key) => ColumnKey.StepStartTime == key;
        
        private string FormatTime(float seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);
            return $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }
    }
}