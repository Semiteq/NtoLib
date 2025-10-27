using System;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Domain;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Protocol;

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
                    .WithMetadata(nameof(Codes), Codes.PlcCapacityExceeded));

        if (requiredFloat > settings.FloatAreaSize)
            return Result.Fail(
                new Error($"FLOAT area capacity exceeded: need {requiredFloat}, available {settings.FloatAreaSize}")
                    .WithMetadata(nameof(Codes), Codes.PlcCapacityExceeded));

        return Result.Ok();
    }

    public Result ValidateReadCapacity(int rowCount)
    {
        if (rowCount < 0)
            return Result.Fail(new Error("Invalid negative row count")
                .WithMetadata(nameof(Codes), Codes.PlcReadFailed));
            
        if (rowCount == 0)
            return Result.Ok();

        var settings = _optionsProvider.GetCurrent();
        var requiredInt = rowCount * _layout.IntColumnCount;
        var requiredFloat = rowCount * _layout.FloatColumnCount * 2;

        if (requiredInt > settings.IntAreaSize)
            return Result.Fail(
                new Error($"INT area read capacity exceeded: need {requiredInt}, available {settings.IntAreaSize}")
                    .WithMetadata(nameof(Codes), Codes.PlcInvalidResponse));

        if (requiredFloat > settings.FloatAreaSize)
            return Result.Fail(
                new Error($"FLOAT area read capacity exceeded: need {requiredFloat}, available {settings.FloatAreaSize}")
                    .WithMetadata(nameof(Codes), Codes.PlcInvalidResponse));

        return Result.Ok();
    }
}