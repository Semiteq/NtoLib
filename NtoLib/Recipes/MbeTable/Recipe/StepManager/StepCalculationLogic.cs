using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager
{
    public static class StepCalculationLogic
    {
        public static (object Value, string Error) CalculateDurationFromSpeed(IReadOnlyDictionary<ColumnKey, PropertyWrapper> context)
        {
            try
            {
                var speed = context[ColumnKey.Speed].GetValue<float>();
                var initialValue = context[ColumnKey.InitialValue].GetValue<float>();
                var setpoint = context[ColumnKey.Setpoint].GetValue<float>();

                if (speed <= 0)
                    return (null, "Скорость должна быть больше нуля");

                var duration = Math.Abs(setpoint - initialValue) / speed;
                return (duration, null);
            }
            catch (Exception ex)
            {
                return (null, $"Ошибка расчета длительности: {ex.Message}");
            }
        }

        public static (object Value, string Error) CalculateSpeedFromDuration(IReadOnlyDictionary<ColumnKey, PropertyWrapper> context)
        {
            try
            {
                var duration = context[ColumnKey.Duration].GetValue<float>();
                var initialValue = context[ColumnKey.InitialValue].GetValue<float>();
                var setpoint = context[ColumnKey.Setpoint].GetValue<float>();

                if (duration <= 0)
                    return (null, "Длительность должна быть больше нуля");

                var speed = Math.Abs(setpoint - initialValue) / duration;
                return (speed, null);
            }
            catch (Exception ex)
            {
                return (null, $"Ошибка расчета скорости: {ex.Message}");
            }
        }
    }
}