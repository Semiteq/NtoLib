using System;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Services;

public class StepFactory
{
    private readonly ActionManager _actionManager;
    private readonly TableSchema _tableSchema;
    private readonly PropertyDefinitionRegistry _registry;

    public StepFactory(ActionManager actionManager, TableSchema tableSchema,
        PropertyDefinitionRegistry propertyDefinitionRegistry)
    {
        _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
        _tableSchema = tableSchema ?? throw new ArgumentNullException(nameof(tableSchema));
        _registry = propertyDefinitionRegistry
                    ?? throw new ArgumentNullException(nameof(propertyDefinitionRegistry));
    }

    #region Heater Actions

    public Step CreatePowerStep(
        int target,
        float setpoint = 10f,
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.Power.Id)
            .WithTarget(target)
            .WithSetpoint(setpoint, PropertyType.Percent)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.Immediate)
            .Build();

    public Step CreatePowerSmoothStep(
        int target,
        float initialValue = 10f,
        float setpoint = 20f,
        float speed = 1f,
        float duration = 600f,
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.PowerSmooth.Id)
            .WithTarget(target)
            .WithInitialValue(initialValue, PropertyType.Percent)
            .WithSetpoint(setpoint, PropertyType.Percent)
            .WithSpeed(speed, PropertyType.PowerSpeed)
            .WithDuration(duration)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.LongLasting)
            .Build();

    public Step CreatePowerWaitStep(
        int target,
        float setpoint = 10f,
        float duration = 60f,
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.PowerWait.Id)
            .WithTarget(target)
            .WithSetpoint(setpoint, PropertyType.Percent)
            .WithDuration(duration)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.LongLasting)
            .Build();

    public Step CreateTemperatureStep(
        int target,
        float setpoint = 500f,
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.Temperature.Id)
            .WithTarget(target)
            .WithSetpoint(setpoint, PropertyType.Temp)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.Immediate)
            .Build();

    public Step CreateTemperatureSmoothStep(
        int target,
        float initialValue = 500f,
        float setpoint = 600f,
        float speed = 10f,
        float duration = 600f,
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.TemperatureSmooth.Id)
            .WithTarget(target)
            .WithInitialValue(initialValue, PropertyType.Temp)
            .WithSetpoint(setpoint, PropertyType.Temp)
            .WithSpeed(speed, PropertyType.TempSpeed)
            .WithDuration(duration)
            .WithComment(comment)
            .Build();

    public Step CreateTemperatureWaitStep(
        int target,
        float setpoint = 500f,
        float speed = 60f,
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.TemperatureWait.Id)
            .WithTarget(target)
            .WithSetpoint(setpoint, PropertyType.Temp)
            .WithDuration(speed)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.LongLasting)
            .Build();

    #endregion

    #region Shutter Actions

    public Step CreateCloseStep(
        int target,
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.Close.Id)
            .WithTarget(target)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.Immediate)
            .Build();

    public Step CreateCloseAllStep(
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.CloseAll.Id)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.Immediate)
            .Build();

    public Step CreateOpenStep(
        int target,
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.Open.Id)
            .WithTarget(target)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.Immediate)
            .Build();

    public Step CreateOpenTimeStep(
        int target,
        float setpoint = 1f,
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.OpenTime.Id)
            .WithTarget(target)
            .WithSetpoint(setpoint, PropertyType.Time)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.LongLasting)
            .Build();

    #endregion

    #region Nitrogen Source Actions

    public Step CreateNitrogenRunSourceStep(
        int target,
        float setpoint = 10f,
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.NRun.Id)
            .WithTarget(target)
            .WithSetpoint(setpoint, PropertyType.Flow)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.Immediate)
            .Build();

    public Step CreateNitrogenSourceCloseStep(
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.NClose.Id)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.Immediate)
            .Build();

    public Step CreateNitrogenSourceVentStep(
        int target,
        float setpoint = 10f,
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.NVent.Id)
            .WithTarget(target)
            .WithSetpoint(setpoint, PropertyType.Flow)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.LongLasting)
            .Build();

    #endregion

    #region Service Actions

    public Step CreatePauseStep(
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.Pause.Id)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.LongLasting)
            .Build();

    public Step CreateForLoopStep(
        int setpoint,
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.ForLoop.Id)
            .WithSetpoint(setpoint, PropertyType.Float)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.Immediate)
            .Build();

    public Step CreateEndForLoopStep(
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.EndForLoop.Id)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.Immediate)
            .Build();

    public Step CreateWaitStep(
        float setpoint = 60f,
        string comment = "")
        => new StepBuilder(_tableSchema, _registry)
            .WithAction(_actionManager.Wait.Id)
            .WithSetpoint(setpoint, PropertyType.Time)
            .WithComment(comment)
            .WithDeployDuration(DeployDuration.LongLasting)
            .Build();

    #endregion
}