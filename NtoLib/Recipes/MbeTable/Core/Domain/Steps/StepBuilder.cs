#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Steps.Definitions;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps
{
    /// <summary>
    /// Provides functionality for building and configuring step instances for usage in the domain context.
    /// </summary>
    public sealed class StepBuilder : IStepBuilder
    {
        private readonly PropertyDefinitionRegistry _registry;
        private readonly TableSchema _schema;
        private readonly Dictionary<ColumnKey, StepProperty?> _properties;
        private DeployDuration _deployDuration;
        
        public StepBuilder(int actionId, IStepDefaultsProvider defaultsProvider, PropertyDefinitionRegistry registry, TableSchema schema)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _properties = defaultsProvider.GetDefaultParameters() ?? throw new ArgumentNullException(nameof(defaultsProvider));
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));

            SetAction(actionId);
            InitializeStep();
        }

        public void InitializeStep()
        {
            var schemaKeys = _schema.GetColumns()
                .Select(c => c.Key).ToArray();
            foreach (var key in schemaKeys)
            {
                if (key == ColumnKey.Action) continue;
                if (!_properties.ContainsKey(key))
                {
                    _properties[key] = null;
                }
            }
        }

        public void SetAction(int actionId)
        {
            _properties[ColumnKey.Action] = new StepProperty(actionId, PropertyType.Enum, _registry);
        }

        public IReadOnlyCollection<ColumnKey> NonNullKeys =>
            _properties.Where(kv => kv.Value is not null).Select(kv => kv.Key).ToArray();

        // Check whether a column can be set for this action
        public bool Supports(ColumnKey key) =>
            _properties.ContainsKey(key);

        public StepBuilder WithOptionalTarget(int? target)
        {
            if (target is null) return this;
            return Supports(ColumnKey.ActionTarget)
                ? WithProperty(ColumnKey.ActionTarget, target.Value, PropertyType.Enum)
                : this;
        }

        public StepBuilder WithOptionalInitialValue(float? value)
        {
            if (value is null) return this;
            return WithOptionalDynamic(ColumnKey.InitialValue, value.Value);
        }

        public StepBuilder WithOptionalSetpoint(float? value)
        {
            if (value is null) return this;
            return WithOptionalDynamic(ColumnKey.Setpoint, value.Value);
        }

        public StepBuilder WithOptionalSpeed(float? value)
        {
            if (value is null) return this;
            return WithOptionalDynamic(ColumnKey.Speed, value.Value);
        }

        public StepBuilder WithOptionalDuration(float? value)
        {
            if (value is null) return this;
            return WithOptionalDynamic(ColumnKey.StepDuration, value.Value);
        }

        public StepBuilder WithOptionalComment(string? comment)
        {
            if (comment is null) return this;
            return WithOptionalDynamic(ColumnKey.Comment, comment);
        }

        public StepBuilder WithDeployDuration(DeployDuration duration)
        {
            _deployDuration = duration;
            return this;
        }

        public Step Build()
        {
            _properties[ColumnKey.StepStartTime] = new StepProperty(0f, PropertyType.Time, _registry);
            return new Step(_properties.ToImmutableDictionary(), _deployDuration);
        }

        public StepBuilder WithProperty(ColumnKey key, object value, PropertyType type)
        {
            _properties[key] = new StepProperty(value, type, _registry);
            return this;
        }

        public StepBuilder WithOptionalDynamic(ColumnKey key, object value)
        {
            if (!_properties.TryGetValue(key, out var existingProperty) || existingProperty is null)
            {
                // silently ignore when property isn't applicable for this action
                return this;
            }
            _properties[key] = new StepProperty(value, existingProperty.Type, _registry);
            return this;
        }
    }
}