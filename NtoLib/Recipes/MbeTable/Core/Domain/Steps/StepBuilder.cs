#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps;

/// <summary>
/// Stateful builder to construct a Step for a specific action using dynamic PropertyTypeId.
/// </summary>
public sealed class StepBuilder : IStepBuilder
{
    private readonly ActionDefinition _actionDefinition;
    private readonly PropertyDefinitionRegistry _registry;
    private readonly TableColumns _columns;
    private readonly Dictionary<ColumnIdentifier, StepProperty?> _properties;
    private readonly IReadOnlyCollection<ColumnIdentifier> _applicableColumnKeys;
    private readonly HashSet<string> _applicableKeysLookup;

    public StepBuilder(ActionDefinition actionDefinition, PropertyDefinitionRegistry registry, TableColumns columns)
    {
        _actionDefinition = actionDefinition ?? throw new ArgumentNullException(nameof(actionDefinition));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _columns = columns ?? throw new ArgumentNullException(nameof(columns));

        _properties = new Dictionary<ColumnIdentifier, StepProperty?>();

        _applicableColumnKeys = _actionDefinition.Columns
            .Select(c => new ColumnIdentifier(c.Key))
            .ToList()
            .AsReadOnly();

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
        foreach (var column in _columns.GetColumns())
            _properties[column.Key] = null;

        // Action property is Enum by convention
        _properties[WellKnownColumns.Action] = new StepProperty(_actionDefinition.Id, "Enum", _registry);

        foreach (var actionColumn in _actionDefinition.Columns)
        {
            var key = new ColumnIdentifier(actionColumn.Key);

            ColumnDefinition schemaColumnDef;
            try
            {
                schemaColumnDef = _columns.GetColumnDefinition(key);
            }
            catch (KeyNotFoundException)
            {
                throw new InvalidOperationException(
                    $"Configuration Error: actionId={_actionDefinition.Id} ('{_actionDefinition.Name}') references column '{key.Value}', which is not defined in ColumnDefs.yaml.");
            }

            var propertyTypeId = actionColumn.PropertyTypeId;
            var sysType = _registry.GetDefinition(propertyTypeId).SystemType;
            object defaultValue = ConvertDefault(actionColumn.DefaultValue, sysType);
            _properties[key] = new StepProperty(defaultValue, propertyTypeId, _registry);
        }
    }

    public IStepBuilder WithProperty(ColumnIdentifier key, object value, Properties.PropertyType type)
    {
        // Backward-compat overload: map enum name to type id string.
        var propertyTypeId = type.ToString();
        _properties[key] = new StepProperty(value, propertyTypeId, _registry);
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

    private static object ConvertDefault(string? raw, Type targetType)
    {
        if (raw == null)
        {
            if (targetType == typeof(string)) return string.Empty;
            if (targetType == typeof(float)) return 0f;
            if (targetType == typeof(int)) return 0;
            if (targetType == typeof(bool)) return false;
            throw new NotSupportedException($"Default value for type {targetType.Name} is not supported.");
        }

        if (targetType == typeof(string)) return raw;

        if (targetType == typeof(int))
        {
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iv)) return iv;
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var fv)) return (int)fv;
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var dv)) return (int)dv;
            if (bool.TryParse(raw, out var bv)) return bv ? 1 : 0;
        }

        if (targetType == typeof(float))
        {
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var fv)) return fv;
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var dv)) return (float)dv;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iv)) return (float)iv;
            if (bool.TryParse(raw, out var bv)) return bv ? 1f : 0f;
        }

        if (targetType == typeof(bool))
        {
            if (bool.TryParse(raw, out var bv)) return bv;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iv)) return iv != 0;
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var fv)) return Math.Abs(fv) > float.Epsilon;
        }

        throw new InvalidCastException($"Cannot convert default value '{raw}' to type {targetType.Name}.");
    }
}