using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using EasyModbus;

using FluentResults;

using NtoLib.Recipes.MbeTable.Config.Domain;
using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Core.Entities;
using NtoLib.Recipes.MbeTable.Core.Properties;
using NtoLib.Recipes.MbeTable.Core.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.Journaling.Errors;

namespace NtoLib.Recipes.MbeTable.RecipeAssemblyService.Strategies;

/// <summary>
/// Assembly strategy for building Recipe from Modbus data.
/// </summary>
public sealed class ModbusAssemblyStrategy
{
    private readonly AppConfiguration _configuration;
    private readonly PropertyDefinitionRegistry _propertyRegistry;
    private readonly IRuntimeOptionsProvider _runtimeOptionsProvider;

    private readonly int _intColumnCount;
    private readonly int _floatColumnCount;
    private readonly ColumnDefinition? _actionColumn;
    private readonly ColumnDefinition? _stepDurationColumn;

    public ModbusAssemblyStrategy(
        AppConfiguration configuration,
        PropertyDefinitionRegistry propertyRegistry,
        IRuntimeOptionsProvider runtimeOptionsProvider)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _propertyRegistry = propertyRegistry ?? throw new ArgumentNullException(nameof(propertyRegistry));
        _runtimeOptionsProvider =
            runtimeOptionsProvider ?? throw new ArgumentNullException(nameof(runtimeOptionsProvider));

        _intColumnCount = CalculateColumnCount("Int");
        _floatColumnCount = CalculateColumnCount("Float");

        _actionColumn = _configuration.Columns.FirstOrDefault(c =>
            c.Key == MandatoryColumns.Action);
        _stepDurationColumn = _configuration.Columns.FirstOrDefault(c =>
            c.Key == MandatoryColumns.StepDuration);
    }

    public Result<Recipe> AssembleFromModbusData(int[] intData, int[] floatData, int rowCount)
    {
        if (intData == null)
            return Result.Fail<Recipe>("Integer data array is null");
        if (floatData == null)
            return Result.Fail<Recipe>("Float data array is null");
        if (rowCount < 0)
            return Result.Fail<Recipe>("Invalid row count");

        if (rowCount == 0)
            return Result.Ok(new Recipe(ImmutableList<Step>.Empty));

        var validationResult = ValidateMandatoryColumns();
        if (validationResult.IsFailed)
            return validationResult.ToResult<Recipe>();

        var steps = new List<Step>(rowCount);

        for (var row = 0; row < rowCount; row++)
        {
            var stepResult = AssembleStep(intData, floatData, row);
            if (stepResult.IsFailed)
            {
                return Result.Fail<Recipe>(new Error($"Failed to assemble step {row}")
                    .WithMetadata(nameof(ErrorCode), ErrorCode.BusinessInvalidOperation)
                    .CausedBy(stepResult.Errors));
            }

            steps.Add(stepResult.Value);
        }

        return Result.Ok(new Recipe(steps.ToImmutableList()));
    }

    private Result<Step> AssembleStep(int[] intData, int[] floatData, int row)
    {
        var actionIdResult = ExtractActionId(intData, row);
        if (actionIdResult.IsFailed)
            return actionIdResult.ToResult<Step>();

        var actionId = actionIdResult.Value;

        if (!_configuration.Actions.TryGetValue(actionId, out var actionDef))
        {
            return Result.Fail<Step>(new Error($"Unknown action ID: {actionId}")
                .WithMetadata(nameof(ErrorCode), ErrorCode.CoreNoActionFound));
        }

        var builder = new StepBuilder(actionDef, _propertyRegistry, _configuration.Columns);

        var settings = _runtimeOptionsProvider.GetCurrent();
        var registerOrder = settings.WordOrder == WordOrder.HighLow
            ? ModbusClient.RegisterOrder.HighLow
            : ModbusClient.RegisterOrder.LowHigh;

        foreach (var column in _configuration.Columns.Where(c => c.PlcMapping != null))
        {
            if (!builder.Supports(column.Key))
                continue;

            var valueResult = ExtractValue(intData, floatData, row, column, registerOrder);
            if (valueResult.IsFailed)
                return valueResult.ToResult<Step>();

            if (valueResult.Value != null)
            {
                var setResult = builder.WithOptionalDynamic(column.Key, valueResult.Value);
                if (setResult.IsFailed)
                    return setResult.ToResult<Step>();
            }
        }

        return Result.Ok(builder.Build());
    }

    private Result<short> ExtractActionId(int[] intData, int row)
    {
        if (_actionColumn?.PlcMapping == null)
            return Result.Fail<short>("Action column has no PLC mapping");

        var intBase = row * _intColumnCount;
        var index = intBase + _actionColumn.PlcMapping.Index;

        if (index < 0 || index >= intData.Length)
            return Result.Fail<short>($"Action index {index} out of range for row {row}");

        return Result.Ok((short)intData[index]);
    }

    private Result<object?> ExtractValue(
        int[] intData,
        int[] floatData,
        int row,
        ColumnDefinition column,
        ModbusClient.RegisterOrder registerOrder)
    {
        var mapping = column.PlcMapping!;

        switch (mapping.Area.ToLowerInvariant())
        {
            case "int":
            {
                var intBase = row * _intColumnCount;
                var index = intBase + mapping.Index;

                if (index < 0 || index >= intData.Length)
                {
                    return Result.Fail<object?>($"Int index {index} out of range for column {column.Key.Value}");
                }

                return Result.Ok<object?>(intData[index]);
            }

            case "float":
            {
                var floatBase = row * _floatColumnCount * 2;
                var index = floatBase + (mapping.Index * 2);

                if (index < 0 || index + 1 >= floatData.Length)
                {
                    return Result.Fail<object?>($"Float index {index} out of range for column {column.Key.Value}");
                }

                var registers = new[] { floatData[index], floatData[index + 1] };
                var value = ModbusClient.ConvertRegistersToFloat(registers, registerOrder);
                return Result.Ok<object?>(value);
            }

            default:
                return Result.Fail<object?>($"Unknown PLC area: {mapping.Area}");
        }
    }

    private Result ValidateMandatoryColumns()
    {
        if (_actionColumn == null)
        {
            return Result.Fail(new Error("Action column not found in configuration")
                .WithMetadata(nameof(ErrorCode), ErrorCode.CoreNoSuchColumn));
        }

        if (_actionColumn.PlcMapping == null)
        {
            return Result.Fail(new Error("Action column has no PLC mapping")
                .WithMetadata(nameof(ErrorCode), ErrorCode.CoreInvalidColumnKey));
        }

        if (_stepDurationColumn == null)
        {
            return Result.Fail(new Error("StepDuration column not found in configuration")
                .WithMetadata(nameof(ErrorCode), ErrorCode.CoreNoSuchColumn));
        }

        return Result.Ok();
    }

    private int CalculateColumnCount(string area)
    {
        var maxIndex = _configuration.Columns
            .Where(c => c.PlcMapping?.Area.Equals(area, StringComparison.OrdinalIgnoreCase) ?? false)
            .Max(c => (int?)c.PlcMapping?.Index) ?? -1;

        return maxIndex + 1;
    }
}