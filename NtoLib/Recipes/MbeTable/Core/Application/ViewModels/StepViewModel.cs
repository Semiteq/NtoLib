#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Presentation.Table;
using NtoLib.Recipes.MbeTable.Schema;

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
            _stepRecord = stepRecord;
            _updatePropertyAction = updatePropertyAction;

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

        public float? InitialValue
        {
            get => _stepRecord.Properties[ColumnKey.InitialValue]?.GetValue<float>();
            set
            {
                if (value == null || InitialValue == value) return;
                _updatePropertyAction(ColumnKey.InitialValue, value);
            }
        }

        public float? Setpoint
        {
            get => _stepRecord.Properties[ColumnKey.Setpoint]?.GetValue<float>();
            set
            {
                if (value == null || Setpoint == value) return;
                _updatePropertyAction(ColumnKey.Setpoint, value);
            }
        }

        public float? Speed
        {
            get => _stepRecord.Properties[ColumnKey.Speed]?.GetValue<float>();
            set
            {
                if (value == null || Speed == value) return;
                _updatePropertyAction(ColumnKey.Speed, value);
            }
        }

        public float? StepDuration
        {
            get => _stepRecord.Properties[ColumnKey.StepDuration]?.GetValue<float>();
            set
            {
                if (value == null || StepDuration == value) return;
                _updatePropertyAction(ColumnKey.StepDuration, value);
            }
        }

        public float? StepStartTime => _stepStartTimeSeconds;

        public string? Comment
        {
            get => _stepRecord.Properties[ColumnKey.Comment]?.GetValue<string>();
            set
            {
                if (value == null || Comment == value) return;
                _updatePropertyAction(ColumnKey.Comment, value);
            }
        }

        public bool IsPropertyDisabled(ColumnKey key) => _stepRecord.Properties[key] == null;

        public bool IsPropertyReadonly(ColumnKey key) => ColumnKey.StepStartTime == key;
    }
}