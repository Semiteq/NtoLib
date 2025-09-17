#nullable enable

using System;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis;

/// <summary>
/// Contains functions for business logic calculations.
/// These methods are deterministic and have no side effects.
/// They do not handle data extraction or validation, only the core math.
/// </summary>
public class StepCalculationLogic
{
    public Result<float> CalculateDurationFromSpeed(float speed, float initialValue, float setpoint)
    {
        if (speed <= 1e-6f)
        {
            return Result.Fail(new CalculationError("Скорость должна быть больше нуля для расчета."));
        }

        var duration = Math.Abs(setpoint - initialValue) / speed;
        return Result.Ok(duration);
    }

    public Result<float> CalculateSpeedFromDuration(float duration, float initialValue, float setpoint)
    {
        if (duration <= 1e-6f)
        {
            return Result.Fail(new CalculationError("Длительность должна быть больше нуля для расчета."));
        }

        var speed = Math.Abs(setpoint - initialValue) / duration;
        return Result.Ok(speed);
    }
}