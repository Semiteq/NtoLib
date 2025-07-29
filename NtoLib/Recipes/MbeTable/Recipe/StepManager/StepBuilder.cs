using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager;

    public class StepBuilder
    {
        private readonly ActionManager _actionManager;
        private readonly Dictionary<ColumnKey, PropertyWrapper> _step;
        
        public StepBuilder(ActionManager actionManager, TableSchema schema)
        {
            _actionManager = actionManager;
            _step = new Dictionary<ColumnKey, PropertyWrapper>();
            
            foreach (var column in schema.GetReadonlyColumns())
            {
                _step[column.Key] = new PropertyWrapper(default, column.PropertyType, true);
            }
        }
        
        public StepBuilder WithAction(int actionId)
        {
            if (_actionManager.GetActionEntryById(actionId, out var actionEntry, out var error))
                return WithProperty(ColumnKey.Action, new(actionId, PropertyType.Enum));
            
            throw new KeyNotFoundException($"Action with ID {actionId} not found: {error}");
        }
        
        public StepBuilder WithTarget(int target)
            => WithProperty(ColumnKey.ActionTarget, new(target, PropertyType.Enum));

        public StepBuilder WithInitialValue(float value, PropertyType type)
            => WithProperty(ColumnKey.InitialValue, new (value, type));

        public StepBuilder WithSetpoint(float value, PropertyType type)
            => WithProperty(ColumnKey.Setpoint, new(value, type));

        public StepBuilder WithSpeed(float value, PropertyType type)
            => WithProperty(ColumnKey.Speed, new(value, type, false));

        public StepBuilder WithDuration(float value)
            => WithProperty(ColumnKey.Duration, new(value, PropertyType.Time, false));

        public StepBuilder WithComment(string comment)
        {
            if (!string.IsNullOrEmpty(comment))
                WithProperty(ColumnKey.Comment, new(comment, PropertyType.String, false));
            return this;
        }

        public bool TryBuild(out Step step, out string error)
        {
            step = new Step();

            foreach (var param in _step)
            {
                if (!step.TryChangeProperty(param.Key, param.Value.PropertyValue, out error))
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