#nullable enable

using System;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Utils;

/// <summary>
/// Capacity check helper for PLC memory areas, based on dynamic column configuration.
/// </summary>
public class PlcCapacityCalculator
{
    private readonly TableColumns _tableColumns;

    public PlcCapacityCalculator(TableColumns tableColumns)
    {
        _tableColumns = tableColumns ?? throw new ArgumentNullException(nameof(tableColumns));
    }

    public Result TryCheckCapacity(Recipe recipe, CommunicationSettings communicationSettings)
    {
        var rows = recipe.Steps.Count;

        var intsPerRow = GetColumnCountForArea("Int");
        var floatsPerRow = GetColumnCountForArea("Float"); // Number of float variables, not registers

        var requiredIntRegisters = rows * intsPerRow;
        var requiredFloatRegisters = rows * floatsPerRow * 2; // Each float uses 2 registers

        if (requiredIntRegisters > communicationSettings.IntAreaSize)
            return Result.Fail($"INT area capacity exceeded: need {requiredIntRegisters}, available {communicationSettings.IntAreaSize}.");
        
        if (requiredFloatRegisters > communicationSettings.FloatAreaSize)
            return Result.Fail($"FLOAT area capacity exceeded: need {requiredFloatRegisters}, available {communicationSettings.FloatAreaSize}.");

        return Result.Ok();
    }

    private int GetColumnCountForArea(string area)
    {
        var maxIndex = _tableColumns.GetColumns()
            .Where(c => c.PlcMapping?.Area.Equals(area, StringComparison.OrdinalIgnoreCase) ?? false)
            .Max(c => (int?)c.PlcMapping?.Index) ?? -1;
        
        return maxIndex + 1;
    }
}