using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;

public sealed class StepViewModel
{
    private Step _step;
    private int _rowIndex;
    private TimeSpan _startTime;
    private readonly IComboboxDataProvider _comboboxDataProvider;

    public StepViewModel(
        Step step,
        int rowIndex,
        TimeSpan startTime,
        IComboboxDataProvider comboboxDataProvider)
    {
        _step = step ?? throw new ArgumentNullException(nameof(step));
        _comboboxDataProvider = comboboxDataProvider ?? throw new ArgumentNullException(nameof(comboboxDataProvider));
        _rowIndex = rowIndex;
        _startTime = startTime;
    }

    public string StepStartTime => FormatTime(_startTime);

    public Result<short> GetCurrentActionId()
    {
        if (!_step.Properties.TryGetValue(MandatoryColumns.Action, out var actionProperty))
        {
            return Result.Fail(new Error("Step does not have Action property")
                .WithMetadata("code", Codes.CoreNoActionFound)
                .WithMetadata("rowIndex", _rowIndex));
        }

        if (actionProperty == null)
        {
            return Result.Fail(new Error("Step Action property is null")
                .WithMetadata("code", Codes.CoreNoActionFound)
                .WithMetadata("rowIndex", _rowIndex));
        }

        try
        {
            return Result.Ok(actionProperty.GetValue<short>());
        }
        catch (InvalidCastException ex)
        {
            return Result.Fail(new Error($"Failed to cast Action property to int: {ex.Message}")
                .WithMetadata("code", Codes.PropertyConversionFailed)
                .WithMetadata("rowIndex", _rowIndex));
        }
    }

    public Result<object?> GetPropertyValue(ColumnIdentifier identifier)
    {
        if (identifier == MandatoryColumns.StepStartTime)
            return Result.Ok<object?>(StepStartTime);

        if (!_step.Properties.TryGetValue(identifier, out var property))
        {
            // Log all available properties for debugging
            var availableProps = string.Join(", ", _step.Properties.Keys.Select(k => k.Value));
            
            return Result.Fail(new Error($"Property '{identifier.Value}' not found in step")
                .WithMetadata("code", Codes.CorePropertyNotFound)
                .WithMetadata("rowIndex", _rowIndex)
                .WithMetadata("propertyKey", identifier.Value)
                .WithMetadata("availableProperties", availableProps));
        }

        if (property == null)
            return Result.Ok<object?>(null);

        try
        {
            var value = identifier == MandatoryColumns.Action
                ? property.GetValueAsObject()
                : property.GetDisplayValue();

            return Result.Ok<object?>(value);
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error($"Failed to get property value: {ex.Message}")
                .WithMetadata("code", Codes.CellValueRetrievalFailed)
                .WithMetadata("rowIndex", _rowIndex)
                .WithMetadata("propertyKey", identifier.Value));
        }
    }

    public Result<Property?> GetProperty(ColumnIdentifier identifier)
    {
        if (!_step.Properties.ContainsKey(identifier))
        {
            return Result.Fail(new Error($"Property '{identifier.Value}' does not exist in step")
                .WithMetadata("code", Codes.CorePropertyNotFound)
                .WithMetadata("rowIndex", _rowIndex)
                .WithMetadata("propertyKey", identifier.Value));
        }

        _step.Properties.TryGetValue(identifier, out var property);
        return Result.Ok(property);
    }

    public bool HasProperty(ColumnIdentifier key)
    {
        if (key == MandatoryColumns.StepStartTime)
            return true;

        return _step.Properties.TryGetValue(key, out var property) && property != null;
    }

    public Result<IReadOnlyDictionary<short, string>> GetComboItems(ColumnIdentifier key)
    {
        var actionIdResult = GetCurrentActionId();
        if (actionIdResult.IsFailed)
            return actionIdResult.ToResult();

        return _comboboxDataProvider.GetResultEnumOptions(actionIdResult.Value, key.Value);
    }

    internal void UpdateInPlace(Step newStep, int newRowIndex, TimeSpan newStartTime)
    {
        _step = newStep ?? throw new ArgumentNullException(nameof(newStep));
        _rowIndex = newRowIndex;
        _startTime = newStartTime;
    }

    private static string FormatTime(TimeSpan time) =>
        $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";
}