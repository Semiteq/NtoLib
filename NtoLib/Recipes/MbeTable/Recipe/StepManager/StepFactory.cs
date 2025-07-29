using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager;

public class StepFactory
{
    private readonly ActionManager _actionManager;
    private readonly TableSchema _schema;

    public StepFactory(ActionManager actionManager, TableSchema schema)
    {
        _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    public bool TryCreateStep(out Step step, out string errorString, int actionId, Dictionary<ColumnKey, PropertyWrapper> parameters)
    {
        step = new Step();

        foreach (var param in parameters)
        {
            if (!step.TrySetPropertyWrapper(param.Key, param.Value, out errorString))
                return false;
        }

        errorString = string.Empty;
        return true;
    }

    #region Heater Actions

    public bool TryCreatePowerStep(
        out Step step,
        out string stringError,
        int target,
        float setpoint = 10f,
        string comment = "")
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.Power.Id)
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
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.PowerSmooth.Id)
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
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.PowerWait.Id)
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
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.Temperature.Id)
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
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.TemperatureSmooth.Id)
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
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.TemperatureWait.Id)
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
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.Close.Id)
            .WithTarget(target)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateCloseAllStep(
        out Step step,
        out string stringError,
        string comment = "")
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.CloseAll.Id)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateOpenStep(
        out Step step,
        out string stringError,
        int target,
        string comment = "")
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.Open.Id)
            .WithTarget(target)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateOpenTimeStep(
        out Step step,
        out string stringError,
        int target,
        float setpoint = 1f,
        string comment = "")
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.OpenTime.Id)
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
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.NRun.Id)
            .WithTarget(target)
            .WithSetpoint(setpoint, PropertyType.Flow)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateNitrogenSourceCloseStep(
        out Step step,
        out string stringError,
        string comment = "")
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.NClose.Id)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateNitrogenSourceVentStep(
        out Step step,
        out string stringError,
        int target,
        float setpoint = 10f,
        string comment = "")
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.NVent.Id)
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
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.Pause.Id)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateForLoopStep(
        out Step step,
        out string stringError,
        int setpoint,
        string comment = "")
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.ForLoop.Id)
            .WithSetpoint(setpoint, PropertyType.Int)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateEndForLoopStep(
        out Step step,
        out string stringError,
        string comment = "")
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.EndForLoop.Id)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    public bool TryCreateWaitStep(
        out Step step,
        out string stringError,
        float setpoint = 60f,
        string comment = "")
        => new StepBuilder(_actionManager, _schema)
            .WithAction(_actionManager.Wait.Id)
            .WithSetpoint(setpoint, PropertyType.Time)
            .WithComment(comment)
            .TryBuild(out step, out stringError);

    #endregion
}