using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.PropertyUnion;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager
{
    public class Step
    {
        // ColumnKey - Property pairs
        private readonly Dictionary<ColumnKey, Property> _properties = new();

        public Step(ActionEntry actionEntry)
        {
            ActionEntry = actionEntry;
        }
        
        public IReadOnlyDictionary<ColumnKey, Property> ReadOnlyProperties => _properties;
        
        public bool[] BlockedCells;

        public ActionEntry ActionEntry { get; }

        // Nesting level for "For" loops in the recipe
        public int TabulateLevel { get; set; } = 0;
        public DeployDuration DeployDuration { get; protected set; }
        
        public Property TryGetProperty(ColumnKey columnKey) => _properties.TryGetValue(columnKey, out var property)
            ? property
            : throw new KeyNotFoundException($"Column '{columnKey}' does not exist in the step.");

        public bool TrySetProperty(ColumnKey columnKey, Property property, out string error)
        { 
            if (_properties.ContainsKey(columnKey))
            {
                error = $"Column '{columnKey}' already exists in the step.";
                return false;
            }

            _properties[columnKey] = property;
            error = string.Empty;
            return true;
        }
        
        public bool TrySetProperty<T>(ColumnKey columnKey, T value, out string error)
        {
            if (!_properties.TryGetValue(columnKey, out var property))
            {
                error = $"Column '{columnKey}' does not exist in the step.";
                return false;
            }

            return value switch
            {
                bool boolValue => property.SetValue(boolValue, out error),
                int intValue => property.SetValue(intValue, out error),
                float floatValue => property.SetValue(floatValue, out error),
                string stringValue => property.SetValue(stringValue, out error),
                _ => throw new ArgumentException($"Unsupported type: {typeof(T)}")
            };
        }
        
        public bool TrySetProperty(ColumnKey columnKey, bool value, out string error)
            => TrySetProperty<bool>(columnKey, value, out error);

        public bool TrySetProperty(ColumnKey columnKey, string value, out string error)
            => TrySetProperty<string>(columnKey, value, out error);

        public bool TrySetProperty(ColumnKey columnKey, int value, out string error)
            => TrySetProperty<int>(columnKey, value, out error);

        public bool TrySetProperty(ColumnKey columnKey, float value, out string error)
            => TrySetProperty<float>(columnKey, value, out error);
        
        public bool TrySetAction(int value, out string errorString) =>
            TrySetProperty(ColumnKey.Action, value, out errorString);
    }
}