using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Services;

/// <summary>
/// Stateful builder to construct a Step for a specific action using dynamic PropertyTypeId.
/// </summary>
public sealed class StepBuilder
{
    private readonly ActionDefinition _actionDefinition;
    private readonly PropertyDefinitionRegistry _propertyRegistry;
    private readonly IReadOnlyList<ColumnDefinition> _tableColumns;
    private readonly Dictionary<ColumnIdentifier, Property?> _properties;
    private readonly HashSet<string> _applicableKeysLookup;

    /// <summary>
    /// Gets a collection of column keys that are active (not null) for the current action.
    /// </summary>
    public IReadOnlyCollection<ColumnIdentifier> NonNullKeys { get; }

    public StepBuilder(
        ActionDefinition actionDefinition,
        PropertyDefinitionRegistry propertyRegistry,
        IReadOnlyList<ColumnDefinition> tableColumns)
    {
        _actionDefinition = actionDefinition ?? throw new ArgumentNullException(nameof(actionDefinition));
        _propertyRegistry = propertyRegistry ?? throw new ArgumentNullException(nameof(propertyRegistry));
        _tableColumns = tableColumns ?? throw new ArgumentNullException(nameof(tableColumns));

        _properties = new Dictionary<ColumnIdentifier, Property?>();
        NonNullKeys = _actionDefinition.Columns
            .Select(c => new ColumnIdentifier(c.Key))
            .ToList()
            .AsReadOnly();

        _applicableKeysLookup = new HashSet<string>(
            _actionDefinition.Columns.Select(c => c.Key),
            StringComparer.OrdinalIgnoreCase);

        var initResult = InitializeStep();
        if (initResult.IsFailed)
            throw new InvalidOperationException(
                $"Failed to initialize StepBuilder: {initResult.Errors.First().Message}");
    }

    /// <summary>
    /// Checks whether a specific column is supported (applicable) for the current action.
    /// </summary>
    public bool Supports(ColumnIdentifier key)
    {
        if (key == MandatoryColumns.Action) return true;
        return _applicableKeysLookup.Contains(key.Value);
    }

    /// <summary>
    /// Sets a property with a new value if the property is supported by the action.
    /// Returns Result for error handling.
    /// </summary>
    public Result<StepBuilder> WithOptionalDynamic(ColumnIdentifier key, object value)
    {
        if (!Supports(key))
            return Result.Ok(this);

        if (!_properties.TryGetValue(key, out var existingProperty) || existingProperty == null)
            return Result.Ok(this);

        var propertyResult = existingProperty.WithValue(value);
        if (propertyResult.IsFailed)
        {
            return Result.Fail<StepBuilder>(new Error($"Failed to set property '{key.Value}'")
                .WithMetadata("code", Codes.PropertyValueError)
                .WithMetadata("propertyKey", key.Value)
                .CausedBy(propertyResult.Errors));
        }

        _properties[key] = propertyResult.Value;
        return Result.Ok(this);
    }

    /// <summary>
    /// Constructs and returns the final immutable Step object.
    /// </summary>
    public Step Build() => new(_properties.ToImmutableDictionary(), _actionDefinition.DeployDuration);

    private Result InitializeStep()
    {
        InitializeAllColumnsToNull();

        var actionResult = InitializeActionProperty();
        if (actionResult.IsFailed)
            return actionResult;

        return InitializeActionColumns();
    }

    private void InitializeAllColumnsToNull()
    {
        foreach (var column in _tableColumns)
        {
            _properties[column.Key] = null;
        }
    }

    private Result InitializeActionProperty()
    {
        try
        {
            _properties[MandatoryColumns.Action] = new Property(
                _actionDefinition.Id,
                _propertyRegistry.GetPropertyDefinition("Enum"));
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error(ex.Message)
                .WithMetadata("code", Codes.PropertyValueError));
        }
    }

    private Result InitializeActionColumns()
    {
        foreach (var column in _actionDefinition.Columns)
        {
            var result = InitializeColumn(column);
            if (result.IsFailed)
                return result;
        }

        return Result.Ok();
    }

    private Result InitializeColumn(PropertyConfig column)
    {
        var key = new ColumnIdentifier(column.Key);
        var defaultValueResult = ResolveDefaultValue(column);

        if (defaultValueResult.IsFailed)
            return defaultValueResult.ToResult();

        return CreateAndStoreProperty(key, defaultValueResult.Value, column.PropertyTypeId);
    }

    private Result<object> ResolveDefaultValue(PropertyConfig column)
    {
        var propertyDefinition = _propertyRegistry.GetPropertyDefinition(column.PropertyTypeId);

        if (column.DefaultValue == null)
            return GetSystemTypeDefault(propertyDefinition.SystemType);

        var parseResult = propertyDefinition.TryParse(column.DefaultValue);
        if (parseResult.IsFailed)
        {
            return Result.Fail<object>(
                    new Error($"Invalid default value '{column.DefaultValue}' for column '{column.Key}'")
                        .WithMetadata("code", Codes.PropertyValueError))
                .WithErrors(parseResult.Errors);
        }

        return Result.Ok(parseResult.Value);
    }

    private static Result<object> GetSystemTypeDefault(Type systemType)
    {
        return systemType switch
        {
            Type t when t == typeof(string) => Result.Ok<object>(string.Empty),
            Type t when t == typeof(float) => Result.Ok<object>(0f),
            Type t when t == typeof(short) => Result.Ok<object>((short)0),
            _ => Result.Fail<object>(new Error($"No default value defined for type '{systemType.Name}'")
                .WithMetadata("code", Codes.PropertyValueError))
        };
    }

    private Result CreateAndStoreProperty(ColumnIdentifier key, object defaultValue, string propertyTypeId)
    {
        try
        {
            _properties[key] = new Property(defaultValue, _propertyRegistry.GetPropertyDefinition(propertyTypeId));
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error(ex.Message)
                .WithMetadata("code", Codes.PropertyValueError)
                .WithMetadata("propertyKey", key.Value)
                .WithMetadata("propertyTypeId", propertyTypeId));
        }
    }
}