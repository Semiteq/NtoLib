using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager
{
    public class Step
    {
        public int NestingLevel { get; set; } = 0;
        public IReadOnlyDictionary<ColumnKey, PropertyWrapper> ReadOnlyStep => _step;
        private readonly Dictionary<ColumnKey, PropertyWrapper> _step = new();

        public bool TrySetPropertyWrapper(ColumnKey columnKey, PropertyWrapper propertyWrapper, out string errorString)
        {
            if (propertyWrapper == null)
            {
                errorString = "PropertyWrapper cannot be null.";
                return false;
            }

            _step[columnKey] = propertyWrapper;
            errorString = null;
            return true;
        }

        public bool TryChangePropertyValue<T>(ColumnKey columnKey, T value, out string errorString)
        {
            if (!_step.ContainsKey(columnKey))
                throw new KeyNotFoundException($"ColumnKey {columnKey} not found in step properties.");

            return _step[columnKey].TryChangeValue(value, out errorString);
        }

        public bool TryGetPropertyWrapper(ColumnKey columnKey, out PropertyWrapper propertyWrapper)
        {
            if (!_step.TryGetValue(columnKey, out propertyWrapper))
                return false;

            return true;
        }

        public bool TryGetPropertyValue<T>(ColumnKey columnKey, out T value)
        {
            value = default;
            return _step.TryGetValue(columnKey, out var propertyWrapper)
                   && propertyWrapper.TryGetValue(out value);
        }

        public bool TryGetPropertyFormattedValue(ColumnKey columnKey, out string formattedValue)
        {
            formattedValue = null;
            if (!_step.TryGetValue(columnKey, out var propertyWrapper))
                return false;

            formattedValue = propertyWrapper.ToString();

            return true;
        }
    }
}