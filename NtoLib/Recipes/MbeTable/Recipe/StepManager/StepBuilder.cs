using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager;

    public class StepBuilder
    {
        private readonly ActionManager _actionManager;
        private readonly Dictionary<ColumnKey, PropertyWrapper> _step;
        private readonly PropertyDefinitionRegistry _registry = new();
        
        public StepBuilder(ActionManager actionManager, TableSchema schema)
        {
            _actionManager = actionManager;
            _step = new Dictionary<ColumnKey, PropertyWrapper>();
            
            foreach (var column in schema.GetReadonlyColumns())
            {
                // Initially all properties are blocked
                _step[column.Key] = new PropertyWrapper(_registry);
            }
        }
        
        public StepBuilder WithAction(int actionId)
        {
            if (_actionManager.GetActionEntryById(actionId, out var actionEntry, out var error))
            {
                var propertyValue = new PropertyValue(actionEntry.Id, PropertyType.Enum);
                return WithProperty(ColumnKey.Action, new(propertyValue, _registry));
            }
            throw new KeyNotFoundException($"Action with ID {actionId} not found: {error}");
        }

        public StepBuilder WithTarget(int target)
        {
            var propertyValue = new PropertyValue(target, PropertyType.Enum);
            return WithProperty(ColumnKey.ActionTarget, new(propertyValue, _registry));
        }

        public StepBuilder WithInitialValue(float value, PropertyType type)
        {
            var propertyValue = new PropertyValue(value, type);
            return WithProperty(ColumnKey.InitialValue, new(propertyValue, _registry));
        }

        public StepBuilder WithSetpoint(float value, PropertyType type)
        {
            var propertyValue = new PropertyValue(value, type);
            return WithProperty(ColumnKey.Setpoint, new(propertyValue, _registry));
        }

        public StepBuilder WithSpeed(float value, PropertyType type)
        {
            var propertyValue = new PropertyValue(value, type);
            return WithProperty(ColumnKey.Speed, new(propertyValue, _registry));
        }

        public StepBuilder WithDuration(float value)
        {
            var propertyValue = new PropertyValue(value, PropertyType.Time);
            return WithProperty(ColumnKey.Duration, new(propertyValue, _registry));
        }

        public StepBuilder WithComment(string comment)
        {
            var propertyValue = new PropertyValue(comment, PropertyType.String);
            return WithProperty(ColumnKey.Comment, new(propertyValue, _registry));
        }
        
        
        public bool TryBuild(out Step step, out string error)
        {
            step = new Step();

            foreach (var param in _step)
            {
                if (!step.TrySetPropertyWrapper(param.Key, param.Value, out error))
                    return false;
            }
            
            error = string.Empty;
            return true;
        }
        private StepBuilder WithProperty(ColumnKey key, PropertyWrapper property)
        {
            _step[key] = property;
            return this;
        }
    }