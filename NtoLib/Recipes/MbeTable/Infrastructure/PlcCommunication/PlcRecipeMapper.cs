#nullable enable
using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Steps;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PlcCommunication;

/// <summary>
/// Maps domain steps to PLC registers and back (V1).
/// Int: Action, ActionTarget
/// Float: InitialValue, Setpoint, Speed, StepDuration (each float -> 2 registers: low-word, high-word)
/// Bool: not used
/// </summary>
public sealed class PlcRecipeMapper : IPlcRecipeMapper
{
    private readonly StepFactory _stepFactory;
    private readonly ActionManager _actionManager;

    public PlcRecipeMapper(StepFactory stepFactory, ActionManager actionManager)
    {
        _stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
        _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
    }

    /// <summary>
    /// Converts a list of steps into PLC register data arrays.
    /// </summary>
    /// <param name="steps">The list of steps to be mapped into integer, floating-point, and boolean register data.</param>
    /// <returns>A tuple containing three arrays: integers, floating-point values encoded as integers, and boolean values encoded as integers.</returns>
    public (int[] IntArray, int[] FloatArray, int[] BoolArray) ToRegisters(List<Step> steps)
    {
        var rows = steps.Count;

        var intCols = CommunicationSettings.IntColumNum;     // expected 2
        var floatCols = CommunicationSettings.FloatColumNum; // expected 4
        var boolCols = CommunicationSettings.BoolColumNum;   // expected 0

        var intArray = new int[rows * intCols];
        var floatArray = new int[rows * floatCols * 2];
        var boolArray = boolCols > 0
            ? new int[rows * boolCols / 16 + (rows * boolCols % 16 > 0 ? 1 : 0)]
            : Array.Empty<int>();

        for (int row = 0; row < rows; row++)
        {
            var s = steps[row];

            // Int
            var iBase = row * intCols;
            intArray[iBase + 0] = GetInt(s, ColumnKey.Action) ?? 0;
            intArray[iBase + 1] = GetInt(s, ColumnKey.ActionTarget) ?? 0;
            for (int i = 2; i < intCols; i++) intArray[iBase + i] = 0;

            // Float
            var fBase = row * floatCols * 2;
            WriteFloat(floatArray, fBase + 0, GetFloat(s, ColumnKey.InitialValue) ?? 0f);
            if (floatCols > 1) WriteFloat(floatArray, fBase + 2, GetFloat(s, ColumnKey.Setpoint) ?? 0f);
            if (floatCols > 2) WriteFloat(floatArray, fBase + 4, GetFloat(s, ColumnKey.Speed) ?? 0f);
            if (floatCols > 3) WriteFloat(floatArray, fBase + 6, GetFloat(s, ColumnKey.StepDuration) ?? 0f);
            // extra float columns, if configured, remain zero
        }

        return (intArray, floatArray, boolArray);
    }

    /// <summary>
    /// Maps PLC register data to a list of steps.
    /// </summary>
    /// <param name="intData">Array of integer data representing specific register values.</param>
    /// <param name="floatData">Array of floating-point data representing specific register values.</param>
    /// <param name="rowCount">The number of rows to process based on the provided data.</param>
    /// <returns>A list of steps created from the provided register data.</returns>
    public List<Step> FromRegisters(int[] intData, int[] floatData, int rowCount)
    {
        var intCols = CommunicationSettings.IntColumNum;
        var floatCols = CommunicationSettings.FloatColumNum;

        var steps = new List<Step>(rowCount);

        for (int row = 0; row < rowCount; row++)
        {
            var iBase = row * intCols;
            var fBase = row * floatCols * 2;

            var actionId = SafeGet(intData, iBase + 0);
            var targetId = SafeGet(intData, iBase + 1);

            var initial = floatCols >= 1 ? ReadFloat(floatData, fBase + 0) : 0f;
            var setpoint = floatCols >= 2 ? ReadFloat(floatData, fBase + 2) : 0f;
            var speed = floatCols >= 3 ? ReadFloat(floatData, fBase + 4) : 0f;
            var duration = floatCols >= 4 ? ReadFloat(floatData, fBase + 6) : 0f;

            var builder = _stepFactory.For(actionId)
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

    // Helpers

    private int? GetInt(Step step, ColumnKey key)
        => step.Properties.TryGetValue(key, out var prop) ? prop?.GetValue<int>() : null;

    private float? GetFloat(Step step, ColumnKey key)
        => step.Properties.TryGetValue(key, out var prop) ? prop?.GetValue<float>() : null;

    private void WriteFloat(int[] buffer, int offset, float value)
    {
        var (w0, w1) = PackFloat(value);
        buffer[offset + 0] = w0;
        buffer[offset + 1] = w1;
    }

    private float ReadFloat(int[] buffer, int offset)
        => UnpackFloat(buffer[offset + 0], buffer[offset + 1]);

    // Low word first, then high word (legacy format)
    private (int w0, int w1) PackFloat(float value)
    {
        var bytes = BitConverter.GetBytes(value); // little-endian
        var w0 = BitConverter.ToUInt16(bytes, 0);
        var w1 = BitConverter.ToUInt16(bytes, 2);
        return (w0, w1);
    }

    private float UnpackFloat(int w0, int w1)
    {
        var low = BitConverter.GetBytes((ushort)w0);
        var high = BitConverter.GetBytes((ushort)w1);
        var bytes = new byte[4] { low[0], low[1], high[0], high[1] };
        return BitConverter.ToSingle(bytes, 0);
    }

    private int SafeGet(int[] arr, int index) => index >= 0 && index < arr.Length ? arr[index] : 0;
}