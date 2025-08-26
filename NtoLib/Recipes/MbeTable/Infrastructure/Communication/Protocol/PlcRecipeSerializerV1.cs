#nullable enable
using System;
using System.Collections.Generic;
using EasyModbus;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Steps;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Protocol;

public sealed class PlcRecipeSerializerV1 : IPlcRecipeSerializer
{
    private readonly IStepFactory _stepFactory;
    private readonly ActionManager _actionManager;
    private readonly ModbusClient.RegisterOrder _registerOrder;
    private readonly ICommunicationSettingsProvider _communicationSettingsProvider;

    public PlcRecipeSerializerV1(IStepFactory stepFactory, ActionManager actionManager, ICommunicationSettingsProvider communicationSettingsProvider)
    {
        _stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
        _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
        _communicationSettingsProvider = communicationSettingsProvider ?? throw new ArgumentNullException(nameof(communicationSettingsProvider));
        
        _registerOrder = CommunicationSettings.WordOrder == WordOrder.HighLow
            ? ModbusClient.RegisterOrder.HighLow
            : ModbusClient.RegisterOrder.LowHigh;
    }
    
    private CommunicationSettings CommunicationSettings => _communicationSettingsProvider.GetSettings();

    public (int[] IntArray, int[] FloatArray, int[] BoolArray) ToRegisters(IReadOnlyList<Step> steps)
    {
        var rows = steps.Count;

        var intCols = CommunicationSettings.IntColumNum;
        var floatCols = CommunicationSettings.FloatColumNum;
        var boolCols = CommunicationSettings.BoolColumNum;

        var intArray = new int[rows * intCols];
        var floatArray = new int[rows * floatCols * 2];
        var boolArray = boolCols > 0 ? new int[rows * boolCols] : Array.Empty<int>();

        for (var row = 0; row < rows; row++)
        {
            var s = steps[row];
            var iBase = row * intCols;

            intArray[iBase + 0] = GetInt(s, WellKnownColumns.Action) ?? 0;
            if (intCols > 1) intArray[iBase + 1] = GetInt(s, WellKnownColumns.ActionTarget) ?? 0;

            var fBase = row * floatCols * 2;
            if (floatCols > 0) WriteFloat(floatArray, fBase + 0, GetFloat(s, WellKnownColumns.InitialValue) ?? 0f);
            if (floatCols > 1) WriteFloat(floatArray, fBase + 2, GetFloat(s, WellKnownColumns.Setpoint) ?? 0f);
            if (floatCols > 2) WriteFloat(floatArray, fBase + 4, GetFloat(s, WellKnownColumns.Speed) ?? 0f);
            if (floatCols > 3) WriteFloat(floatArray, fBase + 6, GetFloat(s, WellKnownColumns.StepDuration) ?? 0f);

            if (boolCols > 0)
            {
                var bBase = row * boolCols;
                for (var i = 0; i < boolCols; i++)
                    boolArray[bBase + i] = 0;
            }
        }

        return (intArray, floatArray, boolArray);
    }

    public List<Step> FromRegisters(int[] intData, int[] floatData, int rowCount)
    {
        var intCols = CommunicationSettings.IntColumNum;
        var floatCols = CommunicationSettings.FloatColumNum;
        var steps = new List<Step>(rowCount);

        for (var row = 0; row < rowCount; row++)
        {
            var iBase = row * intCols;
            var fBase = row * floatCols * 2;

            var actionId = SafeGet(intData, iBase + 0);
            var targetId = intCols > 1 ? SafeGet(intData, iBase + 1) : 0;

            var initial = floatCols >= 1 ? ReadFloat(floatData, fBase + 0) : 0f;
            var setpoint = floatCols >= 2 ? ReadFloat(floatData, fBase + 2) : 0f;
            var speed = floatCols >= 3 ? ReadFloat(floatData, fBase + 4) : 0f;
            var duration = floatCols >= 4 ? ReadFloat(floatData, fBase + 6) : 0f;

            var builder = _stepFactory.ForAction(actionId)
                .WithOptionalTarget(targetId)
                .WithOptionalInitialValue(initial)
                .WithOptionalSetpoint(setpoint)
                .WithOptionalSpeed(speed)
                .WithOptionalDuration(duration)
                .WithDeployDuration(_actionManager.GetActionEntryById(actionId).DeployDuration);

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