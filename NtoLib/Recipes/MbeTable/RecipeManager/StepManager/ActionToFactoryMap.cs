using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.RecipeManager.Actions;

namespace NtoLib.Recipes.MbeTable.RecipeManager.StepManager;

public record ActionToFactoryMap
{
    public ActionToFactoryMap(ActionManager actionManager, StepFactory stepFactory)
    {
        _stepCreationMap = new Dictionary<int, Func<int, Step>>
        {
            { actionManager.Close.Id, target => stepFactory.CreateCloseStep(target) },
            { actionManager.Open.Id, target => stepFactory.CreateOpenStep(target) },
            { actionManager.OpenTime.Id, target => stepFactory.CreateOpenTimeStep(target) },
            { actionManager.CloseAll.Id, _ => stepFactory.CreateCloseAllStep() },

            { actionManager.Temperature.Id, target => stepFactory.CreateTemperatureStep(target) },
            { actionManager.TemperatureWait.Id, target => stepFactory.CreateTemperatureWaitStep(target) },
            { actionManager.TemperatureSmooth.Id, target => stepFactory.CreateTemperatureSmoothStep(target) },

            { actionManager.Power.Id, target => stepFactory.CreatePowerStep(target) },
            { actionManager.PowerSmooth.Id, target => stepFactory.CreatePowerSmoothStep(target) },
            { actionManager.PowerWait.Id, target => stepFactory.CreatePowerWaitStep(target) },

            { actionManager.Wait.Id, target => stepFactory.CreateWaitStep(target) },
            { actionManager.ForLoop.Id, target => stepFactory.CreateForLoopStep(target) },
            { actionManager.EndForLoop.Id, _ => stepFactory.CreateEndForLoopStep() },
            { actionManager.Pause.Id, _ => stepFactory.CreatePauseStep() },

            { actionManager.NRun.Id, target => stepFactory.CreateNitrogenRunSourceStep(target) },
            { actionManager.NClose.Id, _ => stepFactory.CreateNitrogenSourceCloseStep() },
            { actionManager.NVent.Id, target => stepFactory.CreateNitrogenSourceVentStep(target) }
        };
    }

    public IReadOnlyDictionary<int, Func<int, Step>> StepCreationMap => _stepCreationMap;

    private readonly Dictionary<int, Func<int, Step>> _stepCreationMap;
}