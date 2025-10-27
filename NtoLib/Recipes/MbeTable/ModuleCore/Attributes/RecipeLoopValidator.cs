using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties;
using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Attributes;

/// <summary>
/// Provides validation and analysis of loop structures within a recipe.
/// This is a stateless service that operates on immutable recipe data.
/// </summary>
public class RecipeLoopValidator
{
    private const int ForLoopActionId = (int)ServiceActions.ForLoop;
    private const int EndForLoopActionId = (int)ServiceActions.EndForLoop;
    private const int MaxLoopDepth = 3;

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
                    if (processResult.GetStatus() == ResultStatus.Warning)
                        return processResult;
                    
                    currentDepth++;
                    break;
                }
                case EndForLoopActionId:
                {
                    var processResult = ProcessForLoopEnd(currentDepth, i, nestingLevels);
                    if (processResult.GetStatus() == ResultStatus.Warning)
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
        IDictionary<int, int> nestingLevels)
    {
        if (currentDepth >= MaxLoopDepth)
            return CreateMaxDepthExceededError(stepIndex);

        nestingLevels[stepIndex] = currentDepth;
        return Result.Ok();
    }

    private static Result<IReadOnlyDictionary<int, int>> ProcessForLoopEnd(
        int currentDepth,
        int stepIndex,
        IDictionary<int, int> nestingLevels)
    {
        if (currentDepth - 1 < 0)
            return CreateUnmatchedEndForLoopError(stepIndex);

        nestingLevels[stepIndex] = currentDepth - 1;
        return Result.Ok();
    }

    private static Result<IReadOnlyDictionary<int, int>> CreateMaxDepthExceededError(int stepIndex)
    {
        return Result.Ok<IReadOnlyDictionary<int, int>>(new Dictionary<int, int>())
            .WithReason(new ValidationIssue(Codes.CoreExeedForLoopDepth).WithMetadata("stepIndex", stepIndex));
    }

    private static Result<IReadOnlyDictionary<int, int>> CreateUnmatchedEndForLoopError(int stepIndex)
    {
        return Result.Ok<IReadOnlyDictionary<int, int>>(new Dictionary<int, int>())
            .WithReason(new ValidationIssue(Codes.CoreForLoopError).WithMetadata("stepIndex", stepIndex));
    }

    private static Result<IReadOnlyDictionary<int, int>> CreateUnmatchedForLoopError(int stepIndex)
    {
        return Result.Ok<IReadOnlyDictionary<int, int>>(new Dictionary<int, int>())
            .WithReason(new ValidationIssue(Codes.CoreForLoopError).WithMetadata("stepIndex", stepIndex));
    }

    private static Result<Property> GetActionPropertyIfExistsInStep(Step step)
    {
        if (!step.Properties.TryGetValue(MandatoryColumns.Action, out var actionProperty) || actionProperty == null)
        {
            var error = new Error("Step does not have an action property.")
                .WithMetadata(nameof(Codes), Codes.CoreActionNotFound);
            return Result.Fail(error);
        }
        return Result.Ok(actionProperty);
    }
}