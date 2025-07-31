using System;
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

        private readonly IStepUpdater _stepUpdater;

        private readonly ComboBoxDataProvider _dataProvider;
        public int RowIndex;
        private readonly IReadOnlyStep _readOnlyStep;

        public event PropertyChangedEventHandler PropertyChanged;

        public StepViewModel(IReadOnlyStep readOnlyStep, IStepUpdater stepUpdater, int rowIndex,
            ComboBoxDataProvider dataProvider)
        {
            _readOnlyStep = readOnlyStep ?? throw new ArgumentNullException(nameof(readOnlyStep));
            _stepUpdater = stepUpdater ?? throw new ArgumentNullException(nameof(stepUpdater));
            RowIndex = rowIndex;
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
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
            return _readOnlyStep.GetProperty(key).GetValue<T>();
        }

        public string GetFormattedValue(ColumnKey key)
        {
            return _readOnlyStep.GetProperty(key).ToString();
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
            return _readOnlyStep.GetProperty(key).IsBlocked;
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