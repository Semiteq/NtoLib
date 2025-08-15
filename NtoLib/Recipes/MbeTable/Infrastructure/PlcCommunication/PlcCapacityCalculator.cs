#nullable enable

using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PlcCommunication;

/// <summary>
/// Capacity check helper for PLC memory areas.
/// </summary>
public class PlcCapacityCalculator
{
    /// <summary>
    /// Checks if the specified recipe can fit into the given PLC memory areas, based on the provided settings.
    /// </summary>
    /// <param name="recipe">The recipe containing steps that need to be written to the PLC.</param>
    /// <param name="settings">The PLC communication settings that specify available memory sizes for different data types.</param>
    /// <returns>A tuple containing a boolean indicating whether the capacity check succeeded and an optional error message string if the check failed.</returns>
    public (bool Ok, string? Error) TryCheckCapacity(Recipe recipe, CommunicationSettings settings)
    {
        var rows = recipe.Steps.Count;

        var intsPerRow = CommunicationSettings.IntColumNum;
        var floatsPerRow = CommunicationSettings.FloatColumNum * 2;
        var boolsPerRow = CommunicationSettings.BoolColumNum > 0
            ? CommunicationSettings.BoolColumNum / 16 + (CommunicationSettings.BoolColumNum % 16 > 0 ? 1 : 0)
            : 0;

        var needInt = rows * intsPerRow;
        var needFloat = rows * floatsPerRow;
        var needBool = rows * boolsPerRow;

        if (needInt > settings.IntAreaSize)
            return (false, $"INT area exceeded: need {needInt}, available {settings.IntAreaSize}");
        if (needFloat > settings.FloatAreaSize)
            return (false, $"FLOAT area exceeded: need {needFloat}, available {settings.FloatAreaSize}");
        if (needBool > settings.BoolAreaSize)
            return (false, $"BOOL area exceeded: need {needBool}, available {settings.BoolAreaSize}");

        return (true, null);
    }
}