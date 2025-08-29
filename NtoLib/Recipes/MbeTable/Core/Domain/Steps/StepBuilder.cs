#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps
{
    /// <inheritdoc />
    public sealed class StepBuilder : IStepBuilder
    {
        private readonly ActionDefinition _actionDefinition;
        private readonly PropertyDefinitionRegistry _registry;
        private readonly TableSchema _schema;
        private readonly Dictionary<ColumnIdentifier, StepProperty?> _properties;
        private readonly IReadOnlyCollection<ColumnIdentifier> _applicableColumnKeys;

        /// <summary>
        /// Initializes a new instance of the <see cref="StepBuilder"/> class for a specific action.
        /// </summary>
        /// <param name="actionDefinition">The configuration-driven definition of the action.</param>
        /// <param name="registry">The registry for property type definitions.</param>
        /// <param name="schema">The table schema defining all possible columns.</param>
        public StepBuilder(ActionDefinition actionDefinition, PropertyDefinitionRegistry registry, TableSchema schema)
        {
            _actionDefinition = actionDefinition ?? throw new ArgumentNullException(nameof(actionDefinition));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));

            _properties = new Dictionary<ColumnIdentifier, StepProperty?>();
            _applicableColumnKeys = _actionDefinition.Columns.Keys
                .Select(c => new ColumnIdentifier(c))
                .ToList()
                .AsReadOnly();

            InitializeStep();
        }

        /// <inheritdoc />
        public IReadOnlyCollection<ColumnIdentifier> NonNullKeys => _applicableColumnKeys;

        /// <inheritdoc />
        public bool Supports(ColumnIdentifier key)
        {
            // The action property itself is always supported.
            if (key == WellKnownColumns.Action) return true;
            return _actionDefinition.Columns.ContainsKey(key.Value);
        }

        /// <inheritdoc />
        public void InitializeStep()
        {
            // 1. Initialize all properties from the schema as null (disabled).
            foreach (var column in _schema.GetColumns())
            {
                _properties[column.Key] = null;
            }

            // 2. Set the mandatory action property.
            _properties[WellKnownColumns.Action] = new StepProperty(_actionDefinition.Id, PropertyType.Enum, _registry);

            // 3. "Activate" properties based on the new "Columns" dictionary from the action definition.
            foreach (var columnEntry in _actionDefinition.Columns)
            {
                var key = new ColumnIdentifier(columnEntry.Key);
                var actionColumnDef = columnEntry.Value;
                var schemaColumnDef = _schema.GetColumnDefinition(key);

                // The semantic property type is now taken directly from the action's configuration.
                var propertyType = actionColumnDef.PropertyType;

                object defaultValue;
                if (actionColumnDef.DefaultValue.HasValue)
                {
                    defaultValue = ConvertJsonElement(actionColumnDef.DefaultValue.Value, schemaColumnDef.SystemType);
                }
                else
                {
                    defaultValue = GetDefaultValueForType(schemaColumnDef.SystemType);
                }

                _properties[key] = new StepProperty(defaultValue, propertyType, _registry);
            }
        }

        /// <inheritdoc />
        public IStepBuilder WithOptionalTarget(int? target)
        {
            if (target.HasValue && Supports(WellKnownColumns.ActionTarget))
            {
                _properties[WellKnownColumns.ActionTarget] = new StepProperty(target.Value, PropertyType.Enum, _registry);
            }
            return this;
        }

        /// <inheritdoc />
        public IStepBuilder WithOptionalInitialValue(float? value)
        {
            if (value.HasValue) WithOptionalDynamic(WellKnownColumns.InitialValue, value.Value);
            return this;
        }

        /// <inheritdoc />
        public IStepBuilder WithOptionalSetpoint(float? value)
        {
            if (value.HasValue) WithOptionalDynamic(WellKnownColumns.Setpoint, value.Value);
            return this;
        }

        /// <inheritdoc />
        public IStepBuilder WithOptionalSpeed(float? value)
        {
            if (value.HasValue) WithOptionalDynamic(WellKnownColumns.Speed, value.Value);
            return this;
        }

        /// <inheritdoc />
        public IStepBuilder WithOptionalDuration(float? value)
        {
            if (value.HasValue) WithOptionalDynamic(WellKnownColumns.StepDuration, value.Value);
            return this;
        }

        /// <inheritdoc />
        public IStepBuilder WithOptionalComment(string? comment)
        {
            if (comment != null) WithOptionalDynamic(WellKnownColumns.Comment, comment);
            return this;
        }

        /// <inheritdoc />
        public IStepBuilder WithProperty(ColumnIdentifier key, object value, PropertyType type)
        {
            _properties[key] = new StepProperty(value, type, _registry);
            return this;
        }

        /// <inheritdoc />
        public IStepBuilder WithOptionalDynamic(ColumnIdentifier key, object value)
        {
            if (Supports(key) && _properties.TryGetValue(key, out var existingProperty) && existingProperty != null)
            {
                var propertyResult = existingProperty.WithValue(value);
                if (propertyResult.IsSuccess)
                {
                    var newProperty = propertyResult.Value;
                    _properties[key] = newProperty;
                }
                else
                {
                    throw new InvalidOperationException($"Failed to set property '{key.Value}': {string.Join(", ", propertyResult.Errors.Select(e => e.Message))}");
                }
            }
            return this;
        }

        /// <inheritdoc />
        public Step Build()
        {
            return new Step(_properties.ToImmutableDictionary(), _actionDefinition.ActionType, _actionDefinition.DeployDuration);
        }

        private object GetDefaultValueForType(Type type)
        {
            if (type == typeof(string)) return string.Empty;
            if (type == typeof(float)) return 0f;
            if (type == typeof(int)) return 0;
            if (type == typeof(bool)) return false;

            // This should not be reached for supported types.
            throw new NotSupportedException($"Default value for type {type.Name} is not supported.");
        }

        private object ConvertJsonElement(JsonElement element, Type targetType)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return targetType == typeof(string) ? element.GetString()! : throw new InvalidCastException($"Cannot convert JSON string to type {targetType.Name}.");
                case JsonValueKind.Number:
                    if (targetType == typeof(int)) return element.GetInt32();
                    if (targetType == typeof(float)) return element.GetSingle();
                    throw new InvalidCastException($"Cannot convert JSON number to type {targetType.Name}.");
                case JsonValueKind.True:
                    return targetType == typeof(bool) ? true : throw new InvalidCastException($"Cannot convert JSON 'true' to type {targetType.Name}.");
                case JsonValueKind.False:
                    return targetType == typeof(bool) ? false : throw new InvalidCastException($"Cannot convert JSON 'false' to type {targetType.Name}.");
                default:
                    throw new InvalidCastException($"Cannot convert JSON value of kind {element.ValueKind} to type {targetType.Name}.");
            }
        }
    }
}