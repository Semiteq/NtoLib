#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.RecipeManager.PropertyDataType;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.RecipeManager.StepManager;

public class StepBuilder
{
    private readonly PropertyDefinitionRegistry _registry;
    private readonly Dictionary<ColumnKey, StepProperty?> _properties;
    private DeployDuration _deployDuration;

    public StepBuilder(TableSchema schema, PropertyDefinitionRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry), @"PropertyDefinitionRegistry cannot be null.");
        
        _properties = new Dictionary<ColumnKey, StepProperty?>();
        foreach (var column in schema.GetReadonlyColumns())
        {
            _properties[column.Key] = null;
        }
    }
    
    public StepBuilder WithAction(int actionId)
    {
        return WithProperty(ColumnKey.Action, new StepProperty(actionId, PropertyType.Enum, _registry));
    }

    public StepBuilder WithTarget(int target)
    {
        return WithProperty(ColumnKey.ActionTarget, new StepProperty(target, PropertyType.Enum, _registry));
    }
    
    public StepBuilder WithDeployDuration(DeployDuration duration)
    {
        _deployDuration = duration;
        return this;
    }

    public StepBuilder WithInitialValue(float value, PropertyType type)
    {
        return WithProperty(ColumnKey.InitialValue, new StepProperty(value, type, _registry));
    }

    public StepBuilder WithSetpoint(float value, PropertyType type)
    {
        return WithProperty(ColumnKey.Setpoint, new StepProperty(value, type, _registry));
    }

    public StepBuilder WithSpeed(float value, PropertyType type)
    {
        return WithProperty(ColumnKey.Speed, new StepProperty(value, type, _registry));
    }

    public StepBuilder WithDuration(float value)
    {
        return WithProperty(ColumnKey.StepDuration, new StepProperty(value, PropertyType.Time, _registry));
    }

    public StepBuilder WithComment(string comment)
    {
        return WithProperty(ColumnKey.Comment, new StepProperty(comment, PropertyType.String, _registry));
    }
    
    public Step Build()
    {
        var immutableProperties = _properties.ToImmutableDictionary();
        return new Step(immutableProperties, _deployDuration);
    }
    
    private StepBuilder WithProperty(ColumnKey key, StepProperty stepProperty)
    {
        _properties[key] = stepProperty;
        return this;
    }
}