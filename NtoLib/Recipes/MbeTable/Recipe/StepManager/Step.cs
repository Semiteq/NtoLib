using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;
using NtoLib.Recipes.MbeTable.Schema;
using OneOf;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager
{
    public class Step
    {
        public int NestingLevel { get; set; } = 0;
        public IReadOnlyDictionary<ColumnKey, PropertyWrapper> ReadOnlyStep => _step;
        private readonly Dictionary<ColumnKey, PropertyWrapper> _step = new();

        public bool TryChangeProperty<T>(ColumnKey columnKey, T value, out string errorString)
        {
            if (!_step.ContainsKey(columnKey))
                throw new KeyNotFoundException($"ColumnKey {columnKey} not found in step properties.");

            return _step[columnKey].TryChangeValue(value, out errorString);
        }
        
        public bool TryGetProperty(ColumnKey columnKey, out PropertyWrapper propertyWrapper)
        {
            if (!_step.TryGetValue(columnKey, out propertyWrapper))
                return false;
            
            return true;
        }

        public bool TryGetValue<T>(ColumnKey columnKey, out T value)
        {
            value = default;
            return _step.TryGetValue(columnKey, out var propertyWrapper) 
                   && propertyWrapper.TryGetValue(out value);
        }
        
        public bool TryGetFormattedValue(ColumnKey columnKey, out string formattedValue)
        {
            formattedValue = null;
            if (!_step.TryGetValue(columnKey, out var propertyWrapper))
                return false;

            formattedValue = propertyWrapper.ToString();
            
            return true;
        }
    }
}