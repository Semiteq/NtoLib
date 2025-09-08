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

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps;

/// <summary>
/// Stateful builder to construct a Step for a specific action.
/// </summary>
public sealed class StepBuilder : IStepBuilder
{
    private readonly ActionDefinition _actionDefinition;
    private readonly PropertyDefinitionRegistry _registry;
    private readonly TableSchema _schema;
    private readonly Dictionary<ColumnIdentifier, StepProperty?> _properties;
    private readonly IReadOnlyCollection<ColumnIdentifier> _applicableColumnKeys;
    private readonly HashSet<string> _applicableKeysLookup;

    public StepBuilder(ActionDefinition actionDefinition, PropertyDefinitionRegistry registry, TableSchema schema)
    {
        _actionDefinition = actionDefinition ?? throw new ArgumentNullException(nameof(actionDefinition));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));

        _properties = new Dictionary<ColumnIdentifier, StepProperty?>();

        _applicableColumnKeys = _actionDefinition.Columns
            .Select(c => new ColumnIdentifier(c.Key))
            .ToList()
            .AsReadOnly();

        // Case-insensitive membership check for convenience
        _applicableKeysLookup = new HashSet<string>(_actionDefinition.Columns.Select(c => c.Key), StringComparer.OrdinalIgnoreCase);

        InitializeStep();
    }

    public IReadOnlyCollection<ColumnIdentifier> NonNullKeys => _applicableColumnKeys;

    public bool Supports(ColumnIdentifier key)
    {
        if (key == WellKnownColumns.Action) return true;
        return _applicableKeysLookup.Contains(key.Value);
    }

    public void InitializeStep()
    {
        // 1. Initialize all properties from the schema as null (disabled).
        foreach (var column in _schema.GetColumns())
            _properties[column.Key] = null;

        // 2. Set the mandatory action property.
        _properties[WellKnownColumns.Action] = new StepProperty(_actionDefinition.Id, PropertyType.Enum, _registry);

        // 3. Activate properties based on action columns list.
        foreach (var actionColumn in _actionDefinition.Columns)
        {
            var key = new ColumnIdentifier(actionColumn.Key);

            // Validate presence in schema for clearer error than KeyNotFoundException
            ColumnDefinition schemaColumnDef;
            try
            {
                schemaColumnDef = _schema.GetColumnDefinition(key);
            }
            catch (KeyNotFoundException)
            {
                throw new InvalidOperationException(
                    $"Configuration Error: actionId={_actionDefinition.Id} ('{_actionDefinition.Name}') references column '{key.Value}', " +
                    "which is not defined in TableSchema.json.");
            }

            var propertyType = actionColumn.PropertyType;

            object defaultValue = actionColumn.DefaultValue.HasValue
                ? ConvertJsonElement(actionColumn.DefaultValue.Value, schemaColumnDef.SystemType)
                : GetDefaultValueForType(schemaColumnDef.SystemType);

            _properties[key] = new StepProperty(defaultValue, propertyType, _registry);
        }
    }

    public IStepBuilder WithProperty(ColumnIdentifier key, object value, PropertyType type)
    {
        _properties[key] = new StepProperty(value, type, _registry);
        return this;
    }

    public IStepBuilder WithOptionalDynamic(ColumnIdentifier key, object value)
    {
        if (Supports(key) && _properties.TryGetValue(key, out var existingProperty) && existingProperty != null)
        {
            var propertyResult = existingProperty.WithValue(value);
            if (propertyResult.IsSuccess)
            {
                _properties[key] = propertyResult.Value;
            }
            else
            {
                throw new InvalidOperationException($"Failed to set property '{key.Value}': {string.Join(", ", propertyResult.Errors.Select(e => e.Message))}");
            }
        }
        return this;
    }

    public Step Build() => new(_properties.ToImmutableDictionary(), _actionDefinition.DeployDuration);

    private static object GetDefaultValueForType(Type type)
    {
        if (type == typeof(string)) return string.Empty;
        if (type == typeof(float)) return 0f;
        if (type == typeof(int)) return 0;
        if (type == typeof(bool)) return false;
        throw new NotSupportedException($"Default value for type {type.Name} is not supported.");
    }

    // Updated to handle string defaults like "0" for numeric/bool types
    private static object ConvertJsonElement(JsonElement element, Type targetType)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
            {
                var s = element.GetString() ?? string.Empty;
                return ConvertFromString(s, targetType);
            }
            case JsonValueKind.Number:
            {
                if (targetType == typeof(int))
                {
                    if (element.TryGetInt32(out var iv)) return iv;
                    // try via double then cast
                    if (element.TryGetDouble(out var dv)) return checked((int)dv);
                }
                if (targetType == typeof(float))
                {
                    if (element.TryGetSingle(out var fv)) return fv;
                    if (element.TryGetDouble(out var dv)) return (float)dv;
                }
                // Fallback to string-based conversion
                return ConvertFromString(element.ToString(), targetType);
            }
            case JsonValueKind.True:
                return ConvertFromString("true", targetType);
            case JsonValueKind.False:
                return ConvertFromString("false", targetType);
            case JsonValueKind.Null:
                return GetDefaultValueForType(targetType);
            default:
                throw new InvalidCastException($"Cannot convert JSON value of kind {element.ValueKind} to type {targetType.Name}.");
        }
    }

    private static object ConvertFromString(string s, Type targetType)
    {
        if (targetType == typeof(string)) return s;

        if (targetType == typeof(int))
        {
            if (int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var iv))
                return iv;
            if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fv))
                return checked((int)fv);
            if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dv))
                return checked((int)dv);
            if (bool.TryParse(s, out var bv)) return bv ? 1 : 0;
        }

        if (targetType == typeof(float))
        {
            if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fv))
                return fv;
            if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dv))
                return (float)dv;
            if (int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var iv))
                return (float)iv;
            if (bool.TryParse(s, out var bv)) return bv ? 1f : 0f;
        }

        if (targetType == typeof(bool))
        {
            if (bool.TryParse(s, out var bv)) return bv;
            if (int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var iv))
                return iv != 0;
            if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fv))
                return Math.Abs(fv) > float.Epsilon;
        }

        throw new InvalidCastException($"Cannot convert default value '{s}' to type {targetType.Name}.");
    }
}