using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager
{
    public class Step : IReadOnlyStep 
    {
        public int NestingLevel { get; set; } = 0;
        public IReadOnlyDictionary<ColumnKey, PropertyWrapper> ReadOnlyStep => _step;
        private readonly Dictionary<ColumnKey, PropertyWrapper> _step = new();

        public void SetPropertyWrapper(ColumnKey columnKey, PropertyWrapper propertyWrapper)
        {
            _step[columnKey] = propertyWrapper ?? throw new ArgumentNullException(nameof(propertyWrapper), 
                $@"PropertyWrapper for ColumnKey {columnKey} cannot be null.");; 
        }
        
        /// <param name="columnKey">The key of the property to change.</param>
        /// <param name="value">The new value to set for the property.</param>
        /// <param name="errorString">Stores validation error.</param>
        public bool TryChangePropertyValue(ColumnKey columnKey, object value, out string errorString)
        {
            if (!_step.TryGetValue(columnKey, out var oldValue))
                throw new KeyNotFoundException($"ColumnKey {columnKey} not found in step properties.");

            if (_step[columnKey].TryChangeValue(value, out errorString))
                return true;

            _step[columnKey] = oldValue;
            return false;
        }

        public PropertyWrapper GetProperty(ColumnKey columnKey)
        {
            if (!_step.TryGetValue(columnKey, out var propertyWrapper))
                throw new KeyNotFoundException($"ColumnKey {columnKey} not found in step properties.");
                
            return propertyWrapper;
        }
    }
}