using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
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

    public RecipeTimeAnalysis Calculate(Recipe recipe)
    {
        var stepStartTimes = new Dictionary<int, TimeSpan>(recipe.Steps.Count);
        var accumulatedTime = TimeSpan.Zero;
        var loopStack = new Stack<(int Iterations, TimeSpan BodyStartTime)>();

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            stepStartTimes[i] = accumulatedTime;
            var step = recipe.Steps[i];
            var actionId = step.Properties[WellKnownColumns.Action]?.GetValue<int>() ?? -1;

            if (actionId == ForLoopActionId)
            {
                // Normalize iteration count (treat <1 as 1).
                var rawIterations = step.Properties[WellKnownColumns.Setpoint]?.GetValue<float>() ?? 1f;
                var iterations = Math.Max(1, (int)Math.Round(rawIterations, MidpointRounding.AwayFromZero));
                loopStack.Push((iterations, accumulatedTime));
                continue;
            }

            if (actionId == EndForLoopActionId)
            {
                if (loopStack.Count == 0)
                    continue; // Or log inconsistency if needed.

                var (iterations, bodyStart) = loopStack.Pop();
                var bodyDuration = accumulatedTime - bodyStart;

                if (iterations > 1 && bodyDuration > TimeSpan.Zero)
                {
                    // Multiply body duration for remaining (iterations - 1) loops.
                    accumulatedTime += TimeSpan.FromTicks(bodyDuration.Ticks * (iterations - 1));
                }

                continue;
            }

            // Regular (non-loop control) step.
            var stepDuration = GetStepDuration(step);
            if (stepDuration > TimeSpan.Zero)
                accumulatedTime += stepDuration;
        }

        return new RecipeTimeAnalysis(accumulatedTime, stepStartTimes.ToImmutableDictionary());
    }

    private static TimeSpan GetStepDuration(Step step)
    {
        if (step.DeployDuration != DeployDuration.LongLasting)
            return TimeSpan.Zero;

        var seconds =
            step.Properties[WellKnownColumns.StepDuration]?.GetValue<float>() ??
            step.Properties[WellKnownColumns.Setpoint]?.GetValue<float>() ??
            0f;

        return seconds > 0f ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero;
    }
}