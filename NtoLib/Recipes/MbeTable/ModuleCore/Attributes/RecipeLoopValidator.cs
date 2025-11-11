using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Errors;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Warnings;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Attributes;

public class RecipeLoopValidator
{
    private const int ForLoopActionId = (int)ServiceActions.ForLoop;
    private const int EndForLoopActionId = (int)ServiceActions.EndForLoop;
    private const int MaxLoopDepth = 3;

    public Result<IReadOnlyDictionary<int, int>> Validate(Recipe recipe)
    {
        var nestingLevels = new Dictionary<int, int>();
        var currentDepth = 0;
        var result = Result.Ok<IReadOnlyDictionary<int, int>>(nestingLevels);

        var forStartStack = new Stack<int>();

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            var step = recipe.Steps[i];
            var actionPropertyResult = GetActionPropertyIfExistsInStep(step);
            if (actionPropertyResult.IsFailed)
                return actionPropertyResult.ToResult();

            var propertyValueResult = actionPropertyResult.Value.GetValue<short>();
            if (propertyValueResult.IsFailed)
                return propertyValueResult.ToResult();

            var actionId = (int)propertyValueResult.Value;

            switch (actionId)
            {
                case ForLoopActionId:
                {
                    forStartStack.Push(i);

                    if (!step.Properties.TryGetValue(MandatoryColumns.Task, out var taskProperty) || taskProperty == null)
                        result.WithReason(new CoreForLoopMissingIterationCountWarning(i));

                    if (currentDepth >= MaxLoopDepth)
                    {
                        result.WithReason(new CoreForLoopMaxDepthExceededWarning(i, MaxLoopDepth));
                    }
                    else
                    {
                        nestingLevels[i] = currentDepth;
                        currentDepth++;
                    }
                    break;
                }
                case EndForLoopActionId:
                {
                    if (forStartStack.Count == 0)
                    {
                        result.WithReason(new CoreForLoopUnmatchedWarning(
                            i,
                            startIndex: null,
                            endIndex: i,
                            details: "Unmatched EndFor"));
                    }
                    else
                    {
                        forStartStack.Pop();

                        if (currentDepth - 1 < 0)
                        {
                            result.WithReason(new CoreForLoopUnmatchedWarning(
                                i,
                                startIndex: null,
                                endIndex: i,
                                details: "Unmatched EndFor"));
                        }
                        else
                        {
                            nestingLevels[i] = currentDepth - 1;
                            currentDepth--;
                        }
                    }
                    break;
                }
                default:
                    nestingLevels[i] = currentDepth;
                    break;
            }
        }

        while (forStartStack.Count > 0)
        {
            var startIndex = forStartStack.Pop();
            result.WithReason(new CoreForLoopUnmatchedWarning(
                startIndex,
                startIndex: startIndex,
                endIndex: null,
                details: "Unclosed ForLoop"));
        }

        return result;
    }

    private static Result<Property> GetActionPropertyIfExistsInStep(Step step)
    {
        return step.Properties.TryGetValue(MandatoryColumns.Action, out var actionProperty) && actionProperty != null
            ? Result.Ok(actionProperty)
            : Result.Fail(new CoreStepNoActionPropertyError());
    }
}