using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager
{
    public class StepViewModel : INotifyPropertyChanged
    {
        private readonly Step _step;
        private readonly IStepUpdater _stepUpdater;
        public int RowIndex;
        

        public event PropertyChangedEventHandler PropertyChanged;

        public StepViewModel(Step step, IStepUpdater stepUpdater, int rowIndex)
        {
            _step = step ?? throw new ArgumentNullException(nameof(step));
            _stepUpdater = stepUpdater ?? throw new ArgumentNullException(nameof(stepUpdater));
            RowIndex = rowIndex; 
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
            if (!_step.TryGetValue(key, out T value))
            {
                throw new InvalidOperationException($"ColumnKey {key} not found in step properties.");
            }
            return value;
        }
        
        public string GetFormattedValue(ColumnKey key)
        {
            if (!_step.TryGetFormattedValue(key, out var formattedValue))
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
            return _step.TryGetProperty(key, out var p) && p.IsBlocked;
        }
        
        public void UpdateRowIndex(int newIndex)
        {
            RowIndex = newIndex;
        }
        
        // --- Properties for DataBinding ---

        public int Action
        {
            get => GetValue<int>(ColumnKey.Action);
            set => SetValue(ColumnKey.Action, value);
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