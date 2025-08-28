#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis;

public class RecipeTimeCalculator : IRecipeTimeCalculator
{
    private const int ForLoopActionId = 120;
    private const int EndForLoopActionId = 130;
    private readonly IActionRepository _actionRepository;

    public RecipeTimeCalculator(IActionRepository actionRepository)
    {
        _actionRepository = actionRepository;
    }

    /// <summary>
    /// Calculates the total duration and step start times for a given recipe based on its steps
    /// and their associated properties, such as action types, durations, and loops.
    /// </summary>
    /// <param name="recipe">The recipe containing a list of steps to be analyzed for timing information.</param>
    /// <returns>A <see cref="RecipeTimeAnalysis"/> object containing the total duration and start times of each step in the recipe.</returns>
    public RecipeTimeAnalysis Calculate(Recipe recipe)
    {
        var stepStartTimes = new Dictionary<int, TimeSpan>();
        var accumulatedTime = TimeSpan.Zero;
        var loopStack = new Stack<(int IterationCount, TimeSpan LoopBodyStartTime)>();

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            stepStartTimes[i] = accumulatedTime;
            var step = recipe.Steps[i];

            var actionId = step.Properties[WellKnownColumns.Action]?.GetValue<int>() ?? -1;

            if (actionId == ForLoopActionId)
            {
                var iterationCount = step.Properties[WellKnownColumns.Setpoint]?.GetValue<float>() ?? 1f;
                loopStack.Push(((int)iterationCount, accumulatedTime));
            }
            // else if (actionId == _actionManager.EndForLoop.Id)
            else if (actionId == EndForLoopActionId)
                if (loopStack.Count > 0)
                {
                    var (iterationCount, loopBodyStartTime) = loopStack.Pop();
                    var loopBodyDuration = accumulatedTime - loopBodyStartTime;

                    if (iterationCount > 1)
                    {
                        accumulatedTime += TimeSpan.FromTicks(loopBodyDuration.Ticks * (iterationCount - 1));
                    }
                }

                else if (step.DeployDuration == DeployDuration.LongLasting)
                {
                    var durationInSeconds = step.Properties[WellKnownColumns.StepDuration]?.GetValue<float>() ??
                                            step.Properties[WellKnownColumns.Setpoint]?.GetValue<float>() ??
                                            0;

                    accumulatedTime += TimeSpan.FromSeconds(durationInSeconds);
                }
        }

        return new RecipeTimeAnalysis(accumulatedTime, stepStartTimes.ToImmutableDictionary());
    }
}