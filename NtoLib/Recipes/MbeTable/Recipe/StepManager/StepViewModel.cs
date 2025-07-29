using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NtoLib.Recipes.MbeTable.Schema;
using NtoLib.Recipes.MbeTable.Table;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager
{
    public class StepViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Part of the MVVM pattern.
        /// Represents the state and behavior of a single row in the recipe table for the UI.
        /// It wraps a data model (Step) and adapts it for data binding, providing simple properties
        /// and change notifications via INotifyPropertyChanged. This class ensures the DataGridView works with clean,
        /// display-ready data instead of complex business objects.
        /// </summary>
        
        private readonly Step _step;
        private readonly IStepUpdater _stepUpdater;
        private readonly ComboBoxDataProvider _dataProvider;
        
        public IReadOnlyDictionary<int, string> ActionTargetSource { get; private set; }
        public int RowIndex;
        

        public event PropertyChangedEventHandler PropertyChanged;

        public StepViewModel(Step step, IStepUpdater stepUpdater, int rowIndex, ComboBoxDataProvider dataProvider)
        {
            _step = step ?? throw new ArgumentNullException(nameof(step));
            _stepUpdater = stepUpdater ?? throw new ArgumentNullException(nameof(stepUpdater));
            RowIndex = rowIndex; 
            _dataProvider = dataProvider; 
            UpdateActionTargetSource();
        }
        
        private void UpdateActionTargetSource()
        {
            _dataProvider.TryGetTargetsForAction(Action, out Dictionary<int, string> ActionTargetSource, out var _ );
            // Уведомляем UI, что источник данных для ComboBox'а изменился
            RaisePropertyChanged(nameof(ActionTargetSource));
        }
        
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private T GetValue<T>(ColumnKey key)
        {
            if (!_step.TryGetPropertyValue(key, out T value))
            {
                throw new InvalidOperationException($"ColumnKey {key} not found in step properties.");
            }
            return value;
        }
        
        public string GetFormattedValue(ColumnKey key)
        {
            if (!_step.TryGetPropertyFormattedValue(key, out var formattedValue))
            {
                throw new InvalidOperationException($"ColumnKey {key} not found in step properties.");
            }
            return formattedValue;
        }

        private void SetValue(ColumnKey columnKey, object value)
        {
            if (_stepUpdater.TrySetStepPropertyByObject(RowIndex, columnKey, value, out var errorString))
            {
                throw new InvalidOperationException($"Failed to set value for column {columnKey}: {errorString}");
            }
        }
    
        public bool IsCellBlocked(ColumnKey key)
        {
            return _step.TryGetPropertyWrapper(key, out var p) && p.IsBlocked;
        }
        
        public void UpdateRowIndex(int newIndex)
        {
            RowIndex = newIndex;
        }
        
        // --- Properties for DataBinding ---

        public int Action
        {
            get => GetValue<int>(ColumnKey.Action);
            set
            {
                SetValue(ColumnKey.Action, value);
                UpdateActionTargetSource();
                SetValue(ColumnKey.ActionTarget, 10); // todo: set default target based on action
            }
        }

        public int ActionTarget
        {
            get => GetValue<int>(ColumnKey.ActionTarget);
            set => SetValue(ColumnKey.ActionTarget, value);
        }

        public float InitialValue
        {
            get => GetValue<float>(ColumnKey.InitialValue);
            set => SetValue(ColumnKey.InitialValue, value);
        }

        public float Setpoint
        {
            //todo: if blocked - returns bool, need to connect smh with T
            get => GetValue<float>(ColumnKey.Setpoint);
            set => SetValue(ColumnKey.Setpoint, value);
        }
        
        public float Speed
        {
            get => GetValue<float>(ColumnKey.Speed);
            set => SetValue(ColumnKey.Speed, value);
        }
        
        public float Duration
        {
            get => GetValue<float>(ColumnKey.Duration);
            set => SetValue(ColumnKey.Duration, value);
        }
        
        public float Time
        {
            get => GetValue<float>(ColumnKey.Time);
            // Setter is intentionally omitted for ReadOnly properties
        }

        public string Comment
        {
            get => GetValue<string>(ColumnKey.Comment);
            set => SetValue(ColumnKey.Comment, value);
        }
    }
}