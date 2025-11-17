using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using EasyModbus;
using FluentResults;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.ServiceCsv.Errors;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Errors;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Strategies;

/// <summary>
/// Builds Recipe from Modbus data using column PLC mapping.
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
        _runtimeOptionsProvider = runtimeOptionsProvider ?? throw new ArgumentNullException(nameof(runtimeOptionsProvider));

        _intColumnCount = CalculateColumnCount("Int");
        _floatColumnCount = CalculateColumnCount("Float");

        _actionColumn = _configuration.Columns.FirstOrDefault(column => column.Key == MandatoryColumns.Action);
        _stepDurationColumn = _configuration.Columns.FirstOrDefault(column => column.Key == MandatoryColumns.StepDuration);
    }

    public Result<Recipe> AssembleFromModbusData(int[] intData, int[] floatData, int rowCount)
    {
        if (rowCount < 0)
            return new AssemblyInvalidRowCountError(rowCount);

        if (rowCount == 0)
            return new Recipe(ImmutableList<Step>.Empty);

        var validationResult = ValidateMandatoryColumns();
        if (validationResult.IsFailed)
            return validationResult.ToResult<Recipe>();

        var steps = new List<Step>(rowCount);

        for (var row = 0; row < rowCount; row++)
        {
            var stepResult = AssembleStep(intData, floatData, row);
            if (stepResult.IsFailed)
                return Result.Fail(new AssemblyStepFailedError(row)).WithErrors(stepResult.Errors);

            steps.Add(stepResult.Value);
        }

        return new Recipe(steps.ToImmutableList());
    }

    private Result<Step> AssembleStep(int[] intData, int[] floatData, int row)
    {
        var actionIdResult = ExtractActionId(intData, row);
        if (actionIdResult.IsFailed)
            return actionIdResult.ToResult<Step>();

        var actionId = actionIdResult.Value;
        if (!_configuration.Actions.TryGetValue(actionId, out var actionDef))
            return new CoreActionNotFoundError(actionId);

        var createBuilderResult = StepBuilder.Create(actionDef, _propertyRegistry, _configuration.Columns);
        if (createBuilderResult.IsFailed)
            return createBuilderResult.ToResult();

        var builder = createBuilderResult.Value;

        var settings = _runtimeOptionsProvider.GetCurrent();
        var registerOrder = settings.WordOrder == WordOrder.HighLow
            ? ModbusClient.RegisterOrder.HighLow
            : ModbusClient.RegisterOrder.LowHigh;

        foreach (var column in _configuration.Columns.Where(c => c.PlcMapping != null))
        {
            if (!builder.Supports(column.Key))
                continue;

            var valueResult = ExtractTypedValue(intData, floatData, row, column, registerOrder);
            if (valueResult.IsFailed)
                return valueResult.ToResult<Step>();

            var value = valueResult.Value;
            if (value != null)
            {
                var setResult = builder.WithOptionalDynamic(column.Key, value);
                if (setResult.IsFailed)
                    return setResult.ToResult<Step>();
            }
        }

        return builder.Build();
    }

    private Result<short> ExtractActionId(int[] intData, int row)
    {
        if (_actionColumn?.PlcMapping == null)
            return new AssemblyMissingPlcMappingError("Action");

        var intBase = row * _intColumnCount;
        var index = intBase + _actionColumn.PlcMapping.Index;

        if (index < 0 || index >= intData.Length)
            return new AssemblyPlcIndexOutOfRangeError(index, row, "Action", "Int");

        return (short)intData[index];
    }

    private Result<object?> ExtractTypedValue(
        int[] intData,
        int[] floatData,
        int row,
        ColumnDefinition column,
        ModbusClient.RegisterOrder registerOrder)
    {
        var mapping = column.PlcMapping!;
        var propDef = _propertyRegistry.GetPropertyDefinition(column.PropertyTypeId);

        switch (mapping.Area.ToLowerInvariant())
        {
            case "int":
            {
                var intBase = row * _intColumnCount;
                var index = intBase + mapping.Index;
                if (index < 0 || index >= intData.Length)
                    return new AssemblyPlcIndexOutOfRangeError(index, row, column.Key.Value, "Int");

                var raw = intData[index];
                if (propDef.SystemType == typeof(short))
                    return (short)raw;
                if (propDef.SystemType == typeof(float))
                    return (float)raw;
                if (propDef.SystemType == typeof(string))
                    return raw.ToString(CultureInfo.InvariantCulture);

                return new CsvInvalidDataError($"Unsupported target system type: {propDef.SystemType.Name}");
            }

            case "float":
            {
                var floatBase = row * _floatColumnCount * 2;
                var index = floatBase + (mapping.Index * 2);
                if (index < 0 || index + 1 >= floatData.Length)
                    return new AssemblyPlcIndexOutOfRangeError(index, row, column.Key.Value, "Float");

                var registers = new[] { floatData[index], floatData[index + 1] };
                var f = ModbusClient.ConvertRegistersToFloat(registers, registerOrder);

                if (propDef.SystemType == typeof(float))
                    return f;
                if (propDef.SystemType == typeof(short))
                    return (short)Math.Round(f);
                if (propDef.SystemType == typeof(string))
                    return f.ToString(CultureInfo.InvariantCulture);

                return new CsvInvalidDataError($"Unsupported target system type: {propDef.SystemType.Name}");
            }

            default:
                return new AssemblyUnknownPlcAreaError(mapping.Area);
        }
    }

    private Result ValidateMandatoryColumns()
    {
        if (_actionColumn == null)
            return new AssemblyMandatoryColumnMissingError("Action");

        if (_actionColumn.PlcMapping == null)
            return new AssemblyMissingPlcMappingError("Action");

        if (_stepDurationColumn == null)
            return new AssemblyMandatoryColumnMissingError("StepDuration");

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