#nullable enable

using System;
using NtoLib.Recipes.MbeTable.RecipeManager.PropertyDataType.Errors;

namespace NtoLib.Recipes.MbeTable.RecipeManager.StepManager
{
    /// <summary>
    /// Contains pure, static functions for business logic calculations.
    /// These methods are deterministic and have no side effects.
    /// They do not handle data extraction or validation, only the core math.
    /// </summary>
    public static class StepCalculationLogic
    {
        public static (float? Value, CalculationError? Error) CalculateDurationFromSpeed(float speed, float initialValue, float setpoint)
        {
            if (speed <= 1e-6f)
            {
                return (null, new CalculationError("Скорость должна быть больше нуля для расчета."));
            }

            var duration = Math.Abs(setpoint - initialValue) / speed;
            return (duration, null);
        }

        public static (float? Value, CalculationError? Error) CalculateSpeedFromDuration(float duration, float initialValue, float setpoint)
        {
            if (duration <= 1e-6f)
            {
                return (null, new CalculationError("Длительность должна быть больше нуля для расчета."));
            }
            
            var speed = Math.Abs(setpoint - initialValue) / duration;
            return (speed, null);
        }
    }
}