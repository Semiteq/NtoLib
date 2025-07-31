using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager;

    public class StepBuilder
    {
        private readonly ActionManager _actionManager;
        private readonly Dictionary<ColumnKey, PropertyWrapper> _step;
        private readonly PropertyDefinitionRegistry _registry;
        
        public StepBuilder(ActionManager actionManager, TableSchema schema, PropertyDefinitionRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry), @"PropertyDefinitionRegistry cannot be null.");
            _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager), @"ActionManager cannot be null.");
            _step = new Dictionary<ColumnKey, PropertyWrapper>();
            
            foreach (var column in schema.GetReadonlyColumns())
            {
                // Initially, all properties are blocked
                _step[column.Key] = new PropertyWrapper(_registry);
            }
        }
        
        public StepBuilder WithAction(int actionId)
        {
            var actionEntry = _actionManager.GetActionEntryById(actionId);
            var propertyValue = new PropertyValue(actionEntry.Id, PropertyType.Enum);
            return WithProperty(ColumnKey.Action, new(propertyValue, _registry));
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
        
        
        public Step Build()
        {
            var step = new Step();

            foreach (var param in _step)
            {
                step.SetPropertyWrapper(param.Key, param.Value);
            }
            
            return step;
        }
        private StepBuilder WithProperty(ColumnKey key, PropertyWrapper property)
        {
            _step[key] = property;
            return this;
        }
    }