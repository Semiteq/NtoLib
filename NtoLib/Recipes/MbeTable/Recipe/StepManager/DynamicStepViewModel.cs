using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Runtime.CompilerServices;
using NtoLib.Recipes.MbeTable.Schema;
using NtoLib.Recipes.MbeTable.Table;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager
{
    public class DynamicStepViewModel : DynamicObject, INotifyPropertyChanged
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

        public DynamicStepViewModel(IReadOnlyStep readOnlyStep, IStepUpdater stepUpdater, int rowIndex,
            ComboBoxDataProvider dataProvider)
        {
            _readOnlyStep = readOnlyStep ?? throw new ArgumentNullException(nameof(readOnlyStep));
            _stepUpdater = stepUpdater ?? throw new ArgumentNullException(nameof(stepUpdater));
            RowIndex = rowIndex;
            _dataProvider = dataProvider;
        }

        public IReadOnlyDictionary<int, string> ActionTargetSource
        {
            get
            {
                var currentActionId = _readOnlyStep.GetProperty(ColumnKey.Action).GetValue<int>();
                _dataProvider.TryGetTargetsForAction(currentActionId, out var targets, out _);
                return targets;
            }
        }

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private T GetValue<T>(ColumnKey columnKey)
        {
            return _readOnlyStep.GetProperty(columnKey).GetValue<T>();
        }

        public string GetFormattedValue(ColumnKey columnKey)
        {
            return _readOnlyStep.GetProperty(columnKey).GetDisplayValue();
        }

        private void SetValue(ColumnKey columnKey, object value)
        {
            if (_stepUpdater.TrySetStepPropertyByObject(RowIndex, columnKey, value, out var errorString))
            {
                throw new InvalidOperationException($"Failed to set value for column {columnKey}: {errorString}");
            }

            _stepUpdater.TrySetStepPropertyByObject(RowIndex, columnKey, value,
                out var error); //todo: display conversion error in UI
        }

        public bool IsCellBlocked(ColumnKey columnKey)
        {
            return _readOnlyStep.GetProperty(columnKey).IsBlocked;
        }

        public void UpdateRowIndex(int newIndex)
        {
            RowIndex = newIndex;
        }

        /// <summary>
        /// Dynamically retrieves the value of a property by its name.
        /// Converts the property name (string) into a strongly-typed enum (ColumnKey)
        /// and fetches the corresponding value from the underlying data model.
        /// </summary>
        /// <param name="binder">Provides information about the property being accessed.</param>
        /// <param name="result">The value of the property, if found.</param>
        /// <returns>True if the property is successfully retrieved; otherwise, throws an exception.</returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (binder.Name == nameof(ActionTargetSource))
            {
                result = this.ActionTargetSource;
                return true;
            }

            if (Enum.TryParse<ColumnKey>(binder.Name, out var key))
            {
                result = _readOnlyStep.GetProperty(key).GetValue();
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Dynamically sets the value of a property by its name.
        /// Converts the property name (string) into a strongly-typed enum (ColumnKey)
        /// and updates the corresponding value in the underlying data model.
        /// </summary>
        /// <param name="binder">Provides information about the property being set.</param>
        /// <param name="value">The new value to assign to the property.</param>
        /// <returns>True if the property is successfully updated; otherwise, returns false.</returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (Enum.TryParse<ColumnKey>(binder.Name, out var key))
            {
                _stepUpdater.TrySetStepPropertyByObject(RowIndex, key, value, out _);
                return true;
            }

            return false;
        }
    }
}