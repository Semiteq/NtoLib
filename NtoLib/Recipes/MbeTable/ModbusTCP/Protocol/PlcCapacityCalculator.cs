using System;

using FluentResults;

using NtoLib.Recipes.MbeTable.Core.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.Journaling.Errors;
using NtoLib.Recipes.MbeTable.ModbusTCP.Domain;

namespace NtoLib.Recipes.MbeTable.ModbusTCP.Protocol;

/// <summary>
/// Validates that a recipe fits into PLC memory areas.
/// </summary>
public sealed class PlcCapacityCalculator
{
    private readonly RecipeColumnLayout _layout;
    private readonly IRuntimeOptionsProvider _optionsProvider;

    public PlcCapacityCalculator(
        RecipeColumnLayout layout,
        IRuntimeOptionsProvider optionsProvider)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
    }

    public Result TryCheckCapacity(Recipe recipe)
    {
        if (recipe is null)
            throw new ArgumentNullException(nameof(recipe));

        var settings = _optionsProvider.GetCurrent();
        var rows = recipe.Steps.Count;

        var requiredInt = rows * _layout.IntColumnCount;
        var requiredFloat = rows * _layout.FloatColumnCount * 2;

        if (requiredInt > settings.IntAreaSize)
            return Result.Fail(
                new Error($"INT area capacity exceeded: need {requiredInt}, available {settings.IntAreaSize}")
                    .WithMetadata(nameof(ErrorCode), ErrorCode.PlcInvalidResponse));

        if (requiredFloat > settings.FloatAreaSize)
            return Result.Fail(
                new Error($"FLOAT area capacity exceeded: need {requiredFloat}, available {settings.FloatAreaSize}")
                    .WithMetadata(nameof(ErrorCode), ErrorCode.PlcInvalidResponse));

        return Result.Ok();
    }
}