using System;
using System.Collections.Generic;

using EasyModbus;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Domain;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Protocol;

/// <summary>
/// Serializes Recipe to PLC register format.
/// </summary>
public sealed class PlcRecipeSerializer
{
    private readonly RecipeColumnLayout _layout;
    private readonly IRuntimeOptionsProvider _optionsProvider;

    public PlcRecipeSerializer(
        RecipeColumnLayout layout,
        IRuntimeOptionsProvider optionsProvider)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
    }

    public Result<(int[] IntArray, int[] FloatArray)> ToRegisters(IReadOnlyList<Step> steps)
    {
        var rowCount = steps.Count;
        if (rowCount == 0)
            return (Array.Empty<int>(), Array.Empty<int>());

        var settings = _optionsProvider.GetCurrent();
        var registerOrder = settings.WordOrder == WordOrder.HighLow
            ? ModbusClient.RegisterOrder.HighLow
            : ModbusClient.RegisterOrder.LowHigh;

        var intSize = rowCount * _layout.IntColumnCount;
        var floatSize = rowCount * _layout.FloatColumnCount * 2;

        var intArr = new int[intSize];
        var floatArr = new int[floatSize];

        for (var row = 0; row < rowCount; row++)
        {
            var step = steps[row];
            foreach (var col in _layout.MappedColumns)
            {
                if (!step.Properties.TryGetValue(col.Key, out var prop) || prop is null)
                    continue;

                var mapping = col.PlcMapping!;
                switch (mapping.Area)
                {
                    case "Int":
                        var getShortResult = prop.GetValue<short>();
                        if (getShortResult.IsFailed) return FailedToGetPropertyError(col.Key);
                        intArr[row * _layout.IntColumnCount + mapping.Index] = getShortResult.Value;
                        break;
                    case "Float":
                        var getFloatResult = prop.GetValue<float>();
                        if (getFloatResult.IsFailed) return FailedToGetPropertyError(col.Key);
                        WriteFloat(floatArr, row * _layout.FloatColumnCount * 2 + mapping.Index * 2,
                            getFloatResult.Value, registerOrder);
                        break;
                }
            }
        }

        return (intArr, floatArr);
    }

    private static void WriteFloat(int[] buffer, int offset, float value, ModbusClient.RegisterOrder order)
    {
        var regs = ModbusClient.ConvertFloatToRegisters(value, order);
        buffer[offset] = regs[0];
        buffer[offset + 1] = regs[1];
    }
    
    private static Result FailedToGetPropertyError(ColumnIdentifier propertyKey)
    {
        return Result.Fail(new Error($"Failed to get property '{propertyKey}' from step.")
            .WithMetadata(nameof(Codes), Codes.PlcRecipeSerializationPropertyNotFound)
            .WithMetadata("propertyKey", propertyKey));
    }
}