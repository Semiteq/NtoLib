using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.PropertyUnion;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager;

    public class StepBuilder
    {
        private readonly ActionManager _actionManager;
        private readonly int _actionId;
        private readonly Dictionary<ColumnKey, Property> _parameters = new();
        
        public StepBuilder(ActionManager actionManager, int actionId)
        {
            _actionManager = actionManager;
            _actionId = actionId;
        }
        
        public StepBuilder WithTarget(int target) 
            => WithProperty(ColumnKey.ActionTarget, new(PropertyType.Enum, target));

        public StepBuilder WithInitialValue(float value, PropertyType type) 
            => WithProperty(ColumnKey.InitialValue, new(type, value));
        
        public StepBuilder WithSetpoint(float value, PropertyType type)     
            => WithProperty(ColumnKey.Setpoint, new(type, value));
        
        public StepBuilder WithSpeed(float value, PropertyType type)                           
            => WithProperty(ColumnKey.Speed, new(type, value));
        
        public StepBuilder WithDuration(float value)                        
            => WithProperty(ColumnKey.Duration, new(PropertyType.Time, value));
        
        public StepBuilder WithComment(string comment)
        {
            if (!string.IsNullOrEmpty(comment))
                WithProperty(ColumnKey.Comment, new(PropertyType.String, comment));
            return this;
        }

        public bool TryBuild(out Step step, out string error)
        {
            step = new Step(_actionManager.GetActionEntryById(_actionId));

            if (!step.TrySetAction(_actionId, out error))
                return false;

            foreach (var param in _parameters)
            {
                if (!step.TrySetProperty(param.Key, param.Value, out error))
                    return false;
            }

            error = string.Empty;
            return true;
        }
        
        private StepBuilder WithProperty(ColumnKey key, Property property)
        {
            _parameters[key] = property;
            return this;
        }
    }