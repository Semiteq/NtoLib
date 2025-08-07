using System;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PlcCommunication;

public class ModBusDeserializer
{
    private readonly ActionManager _actionManager;
    private readonly StepFactory _stepFactory;

    private const int FloatCellsQuantity = 4;
    private const int FloatDataSize = 2; // Each float is represented by two 16-bit addresses 

    private const int IntCellsQuantity = 2; // Each int is represented by one 16-bit address

    public ModBusDeserializer(ActionManager actionManager, StepFactory stepFactory)
    {
        _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
        _stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
    }

    public Step CreateStep(int[] intData, int[] floatData, int[] boolData, int index)
    {
        var actionId = intData[index * IntCellsQuantity];
        var actionTarget = intData[index * IntCellsQuantity + 1];
        var floatValues = ExtractFloatValues(floatData, index);

        var initialValue = floatValues[0];
        var setpoint = floatValues[1];
        var speed = floatValues[2];
        var duration = floatValues[3];
        var comment = string.Empty;

        if (actionId == _actionManager.Power.Id)
            return _stepFactory.CreatePowerStep(actionTarget, initialValue, string.Empty);

        if (actionId == _actionManager.PowerSmooth.Id)
            return _stepFactory.CreatePowerSmoothStep(actionTarget, initialValue, setpoint,
                speed, duration, comment);

        if (actionId == _actionManager.PowerWait.Id)
            return _stepFactory.CreatePowerWaitStep(actionTarget, setpoint, speed, comment);

        if (actionId == _actionManager.Temperature.Id)
            return _stepFactory.CreateTemperatureStep(actionTarget, setpoint, comment);

        if (actionId == _actionManager.TemperatureSmooth.Id)
            return _stepFactory.CreateTemperatureSmoothStep(actionTarget, initialValue,
                setpoint, speed, duration, comment);

        if (actionId == _actionManager.TemperatureWait.Id)
            return _stepFactory.CreateTemperatureWaitStep(actionTarget, setpoint, speed,
                comment);

        if (actionId == _actionManager.Close.Id)
            return _stepFactory.CreateCloseStep(actionTarget, comment);

        if (actionId == _actionManager.CloseAll.Id)
            return _stepFactory.CreateCloseAllStep(comment);

        if (actionId == _actionManager.Open.Id)
            return _stepFactory.CreateOpenStep(actionTarget, comment);

        if (actionId == _actionManager.OpenTime.Id)
            return _stepFactory.CreateOpenTimeStep(actionTarget, setpoint, comment);

        if (actionId == _actionManager.NRun.Id)
            return _stepFactory.CreateNitrogenRunSourceStep(actionTarget, setpoint, comment);

        if (actionId == _actionManager.NVent.Id)
            return _stepFactory.CreateNitrogenSourceVentStep(actionTarget, setpoint, comment);

        if (actionId == _actionManager.Pause.Id)
            return _stepFactory.CreatePauseStep(comment);

        if (actionId == _actionManager.ForLoop.Id)
            return _stepFactory.CreateForLoopStep((int)setpoint, comment);

        if (actionId == _actionManager.EndForLoop.Id)
            return _stepFactory.CreateEndForLoopStep(comment);

        if (actionId == _actionManager.Wait.Id)
            return _stepFactory.CreateWaitStep(setpoint, comment);
        
        throw new NotSupportedException($"Action with ID {actionId} is not supported.");
    }

    private float[] ExtractFloatValues(int[] floatData, int index)
    {
        var floatValues = new float[FloatCellsQuantity];

        for (var i = 0; i < FloatCellsQuantity; i++)
        {
            var baseIndex = index * FloatDataSize * FloatCellsQuantity + i * IntCellsQuantity;
            if (baseIndex + 1 >= floatData.Length)
                break;

            // float is split into two 16-bit integers
            uint raw = (uint)floatData[baseIndex] | ((uint)floatData[baseIndex + 1] << 16);
            floatValues[i] = BitConverter.ToSingle(BitConverter.GetBytes(raw), 0);
        }

        return floatValues;
    }
}