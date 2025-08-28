#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using NtoLib.Recipes.MbeTable.Composition;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps;

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
        _applicableColumnKeys = _actionDefinition.ApplicableColumns
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
        return _applicableColumnKeys.Any(ack => ack.Value == key.Value);
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

        // 3. "Activate" properties applicable to this action and set their default values.
        foreach (var key in _applicableColumnKeys)
        {
            var columnDef = _schema.GetColumnDefinition(key);
            var propertyType = GetPropertyTypeForSystemType(columnDef.SystemType);
            object defaultValue;

            if (_actionDefinition.DefaultValues.TryGetValue(key.Value, out var jsonValue))
            {
                defaultValue = ConvertJsonElement(jsonValue, columnDef.SystemType);
            }
            else
            {
                defaultValue = GetDefaultValueForType(columnDef.SystemType);
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
            // Create a new property of the same type but with the new value.
            _properties[key] = new StepProperty(value, existingProperty.Type, _registry);
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
        return null!;
    }

    private PropertyType GetPropertyTypeForSystemType(Type type)
    {
        if (type == typeof(string)) return PropertyType.String;
        if (type == typeof(float)) return PropertyType.Float;
        if (type == typeof(int)) return PropertyType.Enum;
        if (type == typeof(bool)) return PropertyType.Bool;

        throw new NotSupportedException($"The system type {type.Name} is not mapped to a PropertyType.");
    }

    private object ConvertJsonElement(JsonElement element, Type targetType)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return targetType == typeof(string) ? element.GetString()! : throw new InvalidCastException();
            case JsonValueKind.Number:
                if (targetType == typeof(int)) return element.GetInt32();
                if (targetType == typeof(float)) return element.GetSingle();
                throw new InvalidCastException();
            case JsonValueKind.True:
                return targetType == typeof(bool) ? true : throw new InvalidCastException();
            case JsonValueKind.False:
                return targetType == typeof(bool) ? false : throw new InvalidCastException();
            default:
                throw new InvalidCastException($"Cannot convert JSON value {element} to type {targetType.Name}.");
        }
    }
}