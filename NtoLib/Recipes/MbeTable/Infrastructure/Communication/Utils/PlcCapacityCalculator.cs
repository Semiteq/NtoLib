#nullable enable

using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Utils;

/// <summary>
/// Capacity check helper for PLC memory areas.
/// </summary>
public class PlcCapacityCalculator
{
    public Result TryCheckCapacity(Recipe recipe, CommunicationSettings communicationSettings)
    {
        var rows = recipe.Steps.Count;

        var intsPerRow = communicationSettings.IntColumNum;
        var floatsPerRow = communicationSettings.FloatColumNum * 2;
        var boolsPerRow = communicationSettings.BoolColumNum > 0
            ? communicationSettings.BoolColumNum / 16 + (communicationSettings.BoolColumNum % 16 > 0 ? 1 : 0)
            : 0;

        var needInt = rows * intsPerRow;
        var needFloat = rows * floatsPerRow;
        var needBool = rows * boolsPerRow;

        if (needInt > communicationSettings.IntAreaSize)
            return Result.Fail($"INT area exceeded: need {needInt}, available {communicationSettings.IntAreaSize}");
        if (needFloat > communicationSettings.FloatAreaSize)
            return Result.Fail($"FLOAT area exceeded: need {needFloat}, available {communicationSettings.FloatAreaSize}");
        if (needBool > communicationSettings.BoolAreaSize)
            return Result.Fail($"BOOL area exceeded: need {needBool}, available {communicationSettings.BoolAreaSize}");

        return Result.Ok();
    }
}