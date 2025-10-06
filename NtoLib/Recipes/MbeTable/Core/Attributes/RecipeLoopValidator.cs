using System;
using System.Collections.Generic;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Core.Entities;
using NtoLib.Recipes.MbeTable.Core.Properties;
using NtoLib.Recipes.MbeTable.Journaling.Errors;


namespace NtoLib.Recipes.MbeTable.Core.Attributes;

/// <summary>
/// Provides validation and analysis of loop structures within a recipe.
/// This is a stateless service that operates on immutable recipe data.
/// </summary>
public class RecipeLoopValidator
{
    private const int ForLoopActionId = (int)ServiceActions.ForLoop;
    private const int EndForLoopActionId = (int)ServiceActions.EndForLoop;
    private const int MaxLoopDepth = 3;

    private readonly ILogger _debugLogger;

    public RecipeLoopValidator(ILogger debugLogger)
    {
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
    }

    /// <summary>
    /// Calculates the nesting level for each step and validates the overall loop structure.
    /// </summary>
    /// <param name="recipe">The recipe to analyze.</param>
    /// <returns>A result object containing nesting levels or a validation error.</returns>
    public Result<IReadOnlyDictionary<int, int>> Validate(Recipe recipe)
    {
        var nestingLevels = new Dictionary<int, int>();
        var currentDepth = 0;

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            var step = recipe.Steps[i];
            var actionPropertyResult = GetActionPropertyIfExistsInStep(step);
            if (actionPropertyResult.IsFailed)
                return actionPropertyResult.ToResult();

            var actionId = (int)actionPropertyResult.Value.GetValue<short>();

            switch (actionId)
            {
                case ForLoopActionId:
                {
                    var processResult = ProcessForLoopStart(currentDepth, i, nestingLevels);
                    if (processResult.IsFailed)
                        return processResult;
                    currentDepth++;
                    break;
                }
                case EndForLoopActionId:
                {
                    var processResult = ProcessForLoopEnd(currentDepth, i, nestingLevels);
                    if (processResult.IsFailed)
                        return processResult;
                    currentDepth--;
                    break;
                }
                default:
                    nestingLevels[i] = currentDepth;
                    break;
            }
        }

        if (currentDepth != 0)
        {
            return CreateUnmatchedForLoopError(recipe.Steps.Count);
        }

        return Result.Ok<IReadOnlyDictionary<int, int>>(nestingLevels);
    }

    private static Result<IReadOnlyDictionary<int, int>> ProcessForLoopStart(
        int currentDepth,
        int stepIndex,
        Dictionary<int, int> nestingLevels)
    {
        if (currentDepth >= MaxLoopDepth)
            return CreateMaxDepthExceededError(stepIndex);

        nestingLevels[stepIndex] = currentDepth;
        return Result.Ok();
    }

    private static Result<IReadOnlyDictionary<int, int>> ProcessForLoopEnd(
        int currentDepth,
        int stepIndex,
        Dictionary<int, int> nestingLevels)
    {
        if (currentDepth - 1 < 0)
            return CreateUnmatchedEndForLoopError(stepIndex);

        nestingLevels[stepIndex] = currentDepth - 1;
        return Result.Ok();
    }

    private static Result<IReadOnlyDictionary<int, int>> CreateMaxDepthExceededError(int stepIndex)
    {
        var error = new Error($"Exceeded max loop depth of {MaxLoopDepth}.")
            .WithMetadata("code", ErrorCode.CoreForLoopFailure)
            .WithMetadata("stepIndex", stepIndex);
        return Result.Fail(error);
    }

    private static Result<IReadOnlyDictionary<int, int>> CreateUnmatchedEndForLoopError(int stepIndex)
    {
        var error = new Error("Unmatched 'EndForLoop' found.")
            .WithMetadata("code", ErrorCode.CoreForLoopFailure)
            .WithMetadata("stepIndex", stepIndex);
        return Result.Fail(error);
    }

    private static Result<IReadOnlyDictionary<int, int>> CreateUnmatchedForLoopError(int stepIndex)
    {
        var error = new Error("Unmatched 'ForLoop' found.")
            .WithMetadata("code", ErrorCode.CoreForLoopFailure)
            .WithMetadata("stepIndex", stepIndex);
        return Result.Fail(error);
    }

    private static Result<Property> GetActionPropertyIfExistsInStep(Step step)
    {
        if (!step.Properties.TryGetValue(MandatoryColumns.Action, out var actionProperty) || actionProperty == null)
        {
            var error = new Error("Step does not have an action property.")
                .WithMetadata("code", ErrorCode.CoreNoActionFound);
            return Result.Fail(error);
        }
        return Result.Ok(actionProperty);
    }
}
