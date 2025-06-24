using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.PropertyUnion;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager;

public class StepFactory
{
    private readonly ActionManager _actionManager;

    public StepFactory(ActionManager actionManager)
    {
        _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
    }
    
    public bool TryCreateStep(out Step step, out string error, int actionId, Dictionary<ColumnKey, Property> parameters)
    {
        step = new Step(_actionManager.GetActionEntryById(actionId));

        if (!step.TrySetAction(actionId, out error))
            return false;

        foreach (var param in parameters)
        {
            if (!step.TrySetProperty(param.Key, param.Value, out error))
                return false;
        }

        error = string.Empty;
        return true;
    }

    #region Heater Actions

    public bool TryCreatePowerStep(
        out Step step, 
        out string stringError, 
        int target, 
        float setpoint = 10f,
        string comment = "") 
        => new StepBuilder(_actionManager, _actionManager.Power.Id)
            .WithTarget(target)
            .WithSetpoint(setpoint, PropertyType.Percent)
            .WithComment(comment)
            .TryBuild(out step, out stringError);
    
    public bool TryCreatePowerSmoothStep(
        out Step step, 
        out string stringError, 
        int target,
        float initialValue = 10f, 
        float setpoint = 20f, 
        float speed = 1f, 
        float duration = 600f, 
        string comment = "") 
        => new StepBuilder(_actionManager, _actionManager.PowerSmooth.Id)
            .WithTarget(target)
            .WithInitialValue(initialValue, PropertyType.Percent)
            .WithSetpoint(setpoint, PropertyType.Percent)
            .WithSpeed(speed, PropertyType.PowerSpeed)
            .WithDuration(duration)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreatePowerWaitStep(
        out Step step, 
        out string stringError, 
        int target,
        float setpoint = 10f, 
        float speed = 60f, 
        string comment = "")
        => new StepBuilder(_actionManager, _actionManager.PowerWait.Id)
            .WithTarget(target)
            .WithSetpoint(setpoint, PropertyType.Percent)
            .WithSpeed(speed, PropertyType.PowerSpeed)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateTemperatureStep(
        out Step step, 
        out string stringError, 
        int target, 
        float setpoint = 500f, 
        string comment = "")
        => new StepBuilder(_actionManager, _actionManager.Temperature.Id)
            .WithTarget(target)
            .WithSetpoint(setpoint, PropertyType.Temp)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateTemperatureSmoothStep(
        out Step step, 
        out string stringError, 
        int target,
        float initialValue = 500f, 
        float setpoint = 600f,
        float speed = 10f, 
        float duration = 600f, 
        string comment = "")
        => new StepBuilder(_actionManager, _actionManager.TemperatureSmooth.Id)
            .WithTarget(target)
            .WithInitialValue(initialValue, PropertyType.Temp)
            .WithSetpoint(setpoint, PropertyType.Temp)
            .WithSpeed(speed, PropertyType.PowerSpeed)
            .WithDuration(duration)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateTemperatureWaitStep(
        out Step step, 
        out string stringError, 
        int target,
        float setpoint = 500f, 
        float speed = 60f, 
        string comment = "")
        => new StepBuilder(_actionManager, _actionManager.TemperatureWait.Id)
            .WithTarget(target)
            .WithSetpoint(setpoint, PropertyType.Temp)
            .WithDuration(speed)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    #endregion

    #region Shutter Actions

    public bool TryCreateCloseStep(
        out Step step, 
        out string stringError, 
        int target, 
        string comment = "")
        => new StepBuilder(_actionManager, _actionManager.Close.Id)
            .WithTarget(target)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateCloseAllStep(
        out Step step, 
        out string stringError, 
        string comment = "")
        => new StepBuilder(_actionManager, _actionManager.CloseAll.Id)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateOpenStep(
        out Step step, 
        out string stringError, 
        int target, 
        string comment = "")
        => new StepBuilder(_actionManager, _actionManager.Open.Id)
            .WithTarget(target)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateOpenTimeStep(
        out Step step, 
        out string stringError,
        int target, 
        float setpoint = 1f, 
        string comment = "")
        => new StepBuilder(_actionManager, _actionManager.OpenTime.Id)
            .WithTarget(target)
            .WithSetpoint(setpoint, PropertyType.Time)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    #endregion

    #region Nitrogen Source Actions

    public bool TryCreateNitrogenRunSourceStep(
        out Step step, 
        out string stringError,
        int target, 
        float setpoint = 10f, 
        string comment = "")
        => new StepBuilder(_actionManager, _actionManager.NVent.Id)
            .WithTarget(target)
            .WithSetpoint(setpoint, PropertyType.Flow)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateNitrogenSourceCloseStep(
        out Step step, 
        out string stringError, 
        string comment = "")
        => new StepBuilder(_actionManager, _actionManager.NRun.Id)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateNitrogenSourceVentStep(
        out Step step, 
        out string stringError,
        int target, 
        float setpoint = 10f, 
        string comment = "")
        => new StepBuilder(_actionManager, _actionManager.NVent.Id)
            .WithTarget(target)
            .WithSetpoint(setpoint, PropertyType.Flow)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    #endregion

    #region Service Actions

    public bool TryCreatePauseStep(
        out Step step, 
        out string stringError, 
        string comment = "")
        => new StepBuilder(_actionManager, _actionManager.Pause.Id)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateForLoopStep(
        out Step step, 
        out string stringError, 
        int setpoint, 
        string comment = "")
        => new StepBuilder(_actionManager, _actionManager.ForLoop.Id)
            .WithSetpoint(setpoint, PropertyType.Int)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateEndForLoopStep(
        out Step step, 
        out string stringError, 
        string comment = "")
        => new StepBuilder(_actionManager, _actionManager.EndForLoop.Id)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateWaitStep(
        out Step step, 
        out string stringError, 
        float setpoint = 60f, 
        string comment = "")
        => new StepBuilder(_actionManager, _actionManager.Wait.Id)
            .WithSetpoint(setpoint, PropertyType.Time)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    #endregion
}