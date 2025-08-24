#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis;

public class RecipeTimeCalculator : IRecipeTimeCalculator
{
    private readonly ActionManager _actionManager;

    public RecipeTimeCalculator(ActionManager actionManager)
    {
        _actionManager = actionManager;
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

            var actionId = step.Properties[ColumnKey.Action]?.GetValue<int>() ?? -1;

            if (actionId == _actionManager.ForLoop.Id)
            {
                var iterationCount = step.Properties[ColumnKey.Setpoint]?.GetValue<float>() ?? 1f;
                loopStack.Push(((int)iterationCount, accumulatedTime));
            }
            else if (actionId == _actionManager.EndForLoop.Id)
            {
                if (loopStack.Count > 0)
                {
                    var (iterationCount, loopBodyStartTime) = loopStack.Pop();
                    var loopBodyDuration = accumulatedTime - loopBodyStartTime;

                    if (iterationCount > 1)
                    {
                        accumulatedTime += TimeSpan.FromTicks(loopBodyDuration.Ticks * (iterationCount - 1));
                    }
                }
            }
            else if (step.DeployDuration == DeployDuration.LongLasting)
            {
                var durationInSeconds = step.Properties[ColumnKey.StepDuration]?.GetValue<float>() ??
                                        step.Properties[ColumnKey.Setpoint]?.GetValue<float>() ??
                                        0;

                accumulatedTime += TimeSpan.FromSeconds(durationInSeconds);
            }
        }

        return new RecipeTimeAnalysis(accumulatedTime, stepStartTimes.ToImmutableDictionary());
    }
}