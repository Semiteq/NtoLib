#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using EasyModbus;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Core.Domain.Steps;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Protocol;

public sealed class PlcRecipeSerializer : IPlcRecipeSerializer
{
    private readonly IStepFactory _stepFactory;
    private readonly TableColumns _tableColumns; 
    private readonly ModbusClient.RegisterOrder _registerOrder;

    public PlcRecipeSerializer(
        IStepFactory stepFactory, 
        TableColumns tableColumns, 
        ICommunicationSettingsProvider communicationSettingsProvider)
    {
        _stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
        _tableColumns = tableColumns ?? throw new ArgumentNullException(nameof(tableColumns)); 
        
        if (communicationSettingsProvider is null)
            throw new ArgumentNullException(nameof(communicationSettingsProvider));
        
        var settings = communicationSettingsProvider.GetSettings();
        _registerOrder = settings.WordOrder == WordOrder.HighLow
            ? ModbusClient.RegisterOrder.HighLow
            : ModbusClient.RegisterOrder.LowHigh;
    }

    /// <inheritdoc />
    public (int[] IntArray, int[] FloatArray) ToRegisters(IReadOnlyList<Step> steps)
    {
        var rowCount = steps.Count;
        if (rowCount == 0)
            return (Array.Empty<int>(), Array.Empty<int>());

        var intCols = GetColumnCountForArea("Int");
        var floatCols = GetColumnCountForArea("Float");
        
        var intArray = new int[rowCount * intCols];
        var floatArray = new int[rowCount * floatCols * 2]; // Each float takes 2 registers

        var writableColumns = _tableColumns.GetColumns().Where(c => c.PlcMapping != null).ToList();

        for (var row = 0; row < rowCount; row++)
        {
            var step = steps[row];
            foreach (var column in writableColumns)
            {
                var mapping = column.PlcMapping!;
                if (!step.Properties.TryGetValue(column.Key, out var property) || property is null)
                {
                    continue; // Skip properties not active for this step
                }

                switch (mapping.Area)
                {
                    case "Int":
                        var intBase = row * intCols;
                        intArray[intBase + mapping.Index] = property.GetValue<int>();
                        break;
                    case "Float":
                        var floatBase = row * floatCols * 2;
                        WriteFloat(floatArray, floatBase + (mapping.Index * 2), property.GetValue<float>());
                        break;
                }
            }
        }
        
        return (intArray, floatArray);
    }

    /// <inheritdoc />
    public List<Step> FromRegisters(int[] intData, int[] floatData, int rowCount)
    {
        if (rowCount == 0)
            return new List<Step>();

        var steps = new List<Step>(rowCount);
        
        var intCols = GetColumnCountForArea("Int");
        var floatCols = GetColumnCountForArea("Float");
        
        var mappedColumns = _tableColumns.GetColumns().Where(c => c.PlcMapping != null).ToList();
        var actionColumn = _tableColumns.GetColumnDefinition(WellKnownColumns.Action);

        for (var row = 0; row < rowCount; row++)
        {
            var intBase = row * intCols;
            var actionId = SafeGet(intData, intBase + actionColumn.PlcMapping!.Index);
            var builder = _stepFactory.ForAction(actionId);

            foreach (var column in mappedColumns)
            {
                var mapping = column.PlcMapping!;
                object? value = null;

                switch (mapping.Area)
                {
                    case "Int":
                        value = SafeGet(intData, intBase + mapping.Index);
                        break;
                    case "Float":
                        var floatBase = row * floatCols * 2;
                        value = ReadFloat(floatData, floatBase + (mapping.Index * 2));
                        break;
                }
                
                if (value != null)
                {
                    builder.WithOptionalDynamic(column.Key, value);
                }
            }
            steps.Add(builder.Build());
        }

        return steps;
    }

    private int GetColumnCountForArea(string area)
    {
        var maxIndex = _tableColumns.GetColumns()
            .Where(c => c.PlcMapping?.Area == area)
            .Max(c => c.PlcMapping?.Index) ?? -1;
        
        return maxIndex + 1;
    }

    private void WriteFloat(int[] buffer, int offset, float value)
    {
        var regs = ModbusClient.ConvertFloatToRegisters(value, _registerOrder);
        buffer[offset] = regs[0];
        buffer[offset + 1] = regs[1];
    }

    private float ReadFloat(int[] buffer, int offset)
    {
        var regs = new[] { SafeGet(buffer, offset), SafeGet(buffer, offset + 1) };
        return ModbusClient.ConvertRegistersToFloat(regs, _registerOrder);
    }

    private int SafeGet(int[] arr, int index) 
        => index >= 0 && index < arr.Length 
            ? arr[index] 
            : 0;
}