#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using EasyModbus;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Core.Domain.Steps;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Protocol;

public sealed class PlcRecipeSerializerV1 : IPlcRecipeSerializer
{
    private readonly IStepFactory _stepFactory;
    private readonly IActionRepository _actionRepository;
    private readonly TableSchema _tableSchema; 
    private readonly ModbusClient.RegisterOrder _registerOrder;
    private readonly ICommunicationSettingsProvider _communicationSettingsProvider;

    public PlcRecipeSerializerV1(
        IStepFactory stepFactory, 
        IActionRepository actionRepository,
        TableSchema tableSchema, 
        ICommunicationSettingsProvider communicationSettingsProvider)
    {
        _stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
        _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
        _tableSchema = tableSchema ?? throw new ArgumentNullException(nameof(tableSchema)); 
        _communicationSettingsProvider = communicationSettingsProvider ?? throw new ArgumentNullException(nameof(communicationSettingsProvider));
        
        _registerOrder = CommunicationSettings.WordOrder == WordOrder.HighLow
            ? ModbusClient.RegisterOrder.HighLow
            : ModbusClient.RegisterOrder.LowHigh;
    }
    
    private CommunicationSettings CommunicationSettings => _communicationSettingsProvider.GetSettings();

    /// <inheritdoc />
    public (int[] IntArray, int[] FloatArray, int[] BoolArray) ToRegisters(IReadOnlyList<Step> steps)
    {
        var rowCount = steps.Count;
        var settings = CommunicationSettings;

        // Определяем размеры массивов на основе максимальных индексов в маппинге
        var maxIntIndex = _tableSchema.GetColumns()
            .Where(c => c.PlcMapping?.Area == "Int")
            .Max(c => c.PlcMapping?.Index ?? -1);
        
        var maxFloatIndex = _tableSchema.GetColumns()
            .Where(c => c.PlcMapping?.Area == "Float")
            .Max(c => c.PlcMapping?.Index ?? -1);

        var intCols = maxIntIndex + 1;
        var floatCols = maxFloatIndex + 1;
        // Bool пока не используется, но логика аналогична
        var boolCols = 0; 
        
        var intArray = new int[rowCount * intCols];
        // Каждый float занимает 2 регистра
        var floatArray = new int[rowCount * floatCols * 2]; 
        var boolArray = boolCols > 0 ? new int[rowCount * boolCols] : Array.Empty<int>();

        var writableColumns = _tableSchema.GetColumns().Where(c => c.PlcMapping != null).ToList();

        for (var row = 0; row < rowCount; row++)
        {
            var step = steps[row];
            foreach (var column in writableColumns)
            {
                var mapping = column.PlcMapping!;
                if (!step.Properties.TryGetValue(column.Key, out var property) || property is null)
                {
                    continue; // Пропускаем неактивные для этого шага свойства
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
                    // case "Bool": ... (логика для bool)
                }
            }
        }
        
        return (intArray, floatArray, boolArray);
    }

    /// <inheritdoc />
    public List<Step> FromRegisters(int[] intData, int[] floatData, int rowCount)
    {
        var steps = new List<Step>(rowCount);
        
        var maxIntIndex = _tableSchema.GetColumns()
            .Where(c => c.PlcMapping?.Area == "Int")
            .Max(c => c.PlcMapping?.Index ?? -1);
        
        var maxFloatIndex = _tableSchema.GetColumns()
            .Where(c => c.PlcMapping?.Area == "Float")
            .Max(c => c.PlcMapping?.Index ?? -1);

        var intCols = maxIntIndex + 1;
        var floatCols = maxFloatIndex + 1;
        
        var mappedColumns = _tableSchema.GetColumns().Where(c => c.PlcMapping != null).ToList();
        var actionColumn = _tableSchema.GetColumnDefinition(WellKnownColumns.Action);

        for (var row = 0; row < rowCount; row++)
        {
            var iBase = row * intCols;
            var actionId = SafeGet(intData, iBase + actionColumn.PlcMapping!.Index);

            var builder = _stepFactory.ForAction(actionId);

            foreach (var column in mappedColumns)
            {
                var mapping = column.PlcMapping!;
                object? value = null;

                switch (mapping.Area)
                {
                    case "Int":
                        value = SafeGet(intData, iBase + mapping.Index);
                        break;
                    case "Float":
                        var fBase = row * floatCols * 2;
                        value = ReadFloat(floatData, fBase + (mapping.Index * 2));
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

    public int? GetInt(Step step, ColumnIdentifier key) 
        => step.Properties.TryGetValue(key, out var prop) 
            ? prop?.GetValue<int>() 
            : null;
    public float? GetFloat(Step step, ColumnIdentifier key) 
        => step.Properties.TryGetValue(key, out var prop) 
            ? prop?.GetValue<float>() 
            : null;

    public void WriteFloat(int[] buffer, int offset, float value)
    {
        var regs = ModbusClient.ConvertFloatToRegisters(value, _registerOrder);
        buffer[offset] = regs[0];
        buffer[offset + 1] = regs[1];
    }

    public float ReadFloat(int[] buffer, int offset)
    {
        var regs = new[] { buffer[offset], buffer[offset + 1] };
        return ModbusClient.ConvertRegistersToFloat(regs, _registerOrder);
    }

    public int SafeGet(int[] arr, int index) 
        => index >= 0 && index < arr.Length 
            ? arr[index] 
            : 0;
}