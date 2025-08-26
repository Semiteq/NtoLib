#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
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
        private readonly Dictionary<ColumnIdentifier, StepProperty?> _properties;
        private DeployDuration _deployDuration;
        private IStepDefaultsProvider _defaultsProvider;
        
        public StepBuilder(int actionId, IStepDefaultsProvider defaultsProvider, PropertyDefinitionRegistry registry, TableSchema schema)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _defaultsProvider = defaultsProvider ?? throw new ArgumentNullException(nameof(defaultsProvider));
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
                if (key == WellKnownColumns.Action) continue;
                if (!_properties.ContainsKey(key))
                {
                    _properties[key] = null;
                }
            }
        }

        public void SetAction(int actionId)
        {
            _properties[WellKnownColumns.Action] = new StepProperty(actionId, PropertyType.Enum, _registry);
        }

        public IReadOnlyCollection<ColumnIdentifier> NonNullKeys =>
            _properties.Where(kv => kv.Value is not null).Select(kv => kv.Key).ToArray();

        // Check whether a column can be set for this action
        public bool Supports(ColumnIdentifier key) =>
            _defaultsProvider.GetDefaultParameters().ContainsKey(key);

        public StepBuilder WithOptionalTarget(int? target)
        {
            if (target is null) return this;
            return Supports(WellKnownColumns.ActionTarget)
                ? WithProperty(WellKnownColumns.ActionTarget, target.Value, PropertyType.Enum)
                : this;
        }

        public StepBuilder WithOptionalInitialValue(float? value)
        {
            if (value is null) return this;
            return WithOptionalDynamic(WellKnownColumns.InitialValue, value.Value);
        }

        public StepBuilder WithOptionalSetpoint(float? value)
        {
            if (value is null) return this;
            return WithOptionalDynamic(WellKnownColumns.Setpoint, value.Value);
        }

        public StepBuilder WithOptionalSpeed(float? value)
        {
            if (value is null) return this;
            return WithOptionalDynamic(WellKnownColumns.Speed, value.Value);
        }

        public StepBuilder WithOptionalDuration(float? value)
        {
            if (value is null) return this;
            return WithOptionalDynamic(WellKnownColumns.StepDuration, value.Value);
        }

        public StepBuilder WithOptionalComment(string? comment)
        {
            if (comment is null) return this;
            return WithOptionalDynamic(WellKnownColumns.Comment, comment);
        }

        public StepBuilder WithDeployDuration(DeployDuration duration)
        {
            _deployDuration = duration;
            return this;
        }

        public Step Build()
        {
            _properties[WellKnownColumns.StepStartTime] = new StepProperty(0f, PropertyType.Time, _registry);
            return new Step(_properties.ToImmutableDictionary(), _deployDuration);
        }

        public StepBuilder WithProperty(ColumnIdentifier key, object value, PropertyType type)
        {
            _properties[key] = new StepProperty(value, type, _registry);
            return this;
        }

        public StepBuilder WithOptionalDynamic(ColumnIdentifier key, object value)
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