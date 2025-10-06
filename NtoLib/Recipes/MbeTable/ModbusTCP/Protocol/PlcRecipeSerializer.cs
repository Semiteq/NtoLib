using System;
using System.Collections.Generic;

using EasyModbus;

using NtoLib.Recipes.MbeTable.Core.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.ModbusTCP.Domain;

namespace NtoLib.Recipes.MbeTable.ModbusTCP.Protocol;

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

    public (int[] IntArray, int[] FloatArray) ToRegisters(IReadOnlyList<Step> steps)
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
                        intArr[row * _layout.IntColumnCount + mapping.Index] = prop.GetValue<short>();
                        break;
                    case "Float":
                        WriteFloat(floatArr, row * _layout.FloatColumnCount * 2 + mapping.Index * 2,
                            prop.GetValue<float>(), registerOrder);
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
}