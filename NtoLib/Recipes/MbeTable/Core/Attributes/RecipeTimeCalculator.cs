using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Core.Entities;
using NtoLib.Recipes.MbeTable.Journaling.Errors;

namespace NtoLib.Recipes.MbeTable.Core.Attributes;

/// <summary>
/// Calculates the total execution time and start times for each step in a recipe.
/// Handles loop structures to accurately compute time based on iteration counts.
/// </summary>
public class RecipeTimeCalculator
{
    private const int ForLoopActionId = (int)ServiceActions.ForLoop;
    private const int EndForLoopActionId = (int)ServiceActions.EndForLoop;
    
    private readonly ILogger _debugLogger;

    public RecipeTimeCalculator(ILogger debugLogger)
    {
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
    }

    /// <summary>
    /// Calculates timing information for all steps in the recipe.
    /// </summary>
    /// <param name="recipe">The recipe to analyze.</param>
    /// <returns>Analysis result containing total time and individual step start times.</returns>
    public Result<(TimeSpan, IReadOnlyDictionary<int, TimeSpan>)> Calculate(Recipe recipe)
    {
        var stepStartTimes = new Dictionary<int, TimeSpan>(recipe.Steps.Count);
        var accumulatedTime = TimeSpan.Zero;
        var loopStack = new Stack<(int Iterations, TimeSpan BodyStartTime)>();

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            stepStartTimes[i] = accumulatedTime;
            var step = recipe.Steps[i];
            
            var actionPropertyResult = GetActionPropertyIfExistsInStep(step, i);
            if (actionPropertyResult.IsFailed)
                return actionPropertyResult.ToResult();

            var actionId = actionPropertyResult.Value;

            switch (actionId)
            {
                case ForLoopActionId:
                {
                    var processResult = ProcessForLoopStart(step, accumulatedTime, loopStack, i);
                    if (processResult.IsFailed)
                        return processResult;
                    break;
                }
                case EndForLoopActionId:
                {
                    var processResult = ProcessForLoopEnd(accumulatedTime, loopStack, i);
                    if (processResult.IsFailed)
                        return processResult.ToResult();
                    accumulatedTime = processResult.Value;
                    break;
                }
                default:
                {
                    var processResult = ProcessRegularStep(step, accumulatedTime, i);
                    if (processResult.IsFailed)
                        return processResult.ToResult();
                    accumulatedTime = processResult.Value;
                    break;
                }
            }
        }

        if (loopStack.Count > 0)
            return CreateUnmatchedForLoopError(recipe.Steps.Count);

        return Result.Ok((accumulatedTime, (IReadOnlyDictionary<int, TimeSpan>)stepStartTimes.ToImmutableDictionary()));
    }

    private static Result<int> GetActionPropertyIfExistsInStep(Step step, int stepIndex)
    {
        if (!step.Properties.TryGetValue(MandatoryColumns.Action, out var actionProperty) || actionProperty == null)
        {
            var error = new Error("Step does not have an action property.")
                .WithMetadata("code", ErrorCode.CoreNoActionFound)
                .WithMetadata("stepIndex", stepIndex);
            return Result.Fail(error);
        }

        return Result.Ok((int)actionProperty.GetValue<short>());
    }

    private static Result ProcessForLoopStart(
        Step step,
        TimeSpan accumulatedTime,
        Stack<(int Iterations, TimeSpan BodyStartTime)> loopStack,
        int stepIndex)
    {
        if (!step.Properties.TryGetValue(MandatoryColumns.StepDuration, out var durationProperty) || durationProperty == null)
            return CreateMissingIterationCountError(stepIndex);

        var rawIterations = durationProperty.GetValue<float>();
        var iterations = Math.Max(1, (int)Math.Round(rawIterations, MidpointRounding.AwayFromZero));
        
        if (iterations <= 0)
            return CreateInvalidIterationCountError(stepIndex, rawIterations);

        loopStack.Push((iterations, accumulatedTime));
        return Result.Ok();
    }

    private static Result<TimeSpan> ProcessForLoopEnd(
        TimeSpan accumulatedTime,
        Stack<(int Iterations, TimeSpan BodyStartTime)> loopStack,
        int stepIndex)
    {
        if (loopStack.Count == 0)
            return CreateUnmatchedEndForLoopError(stepIndex);

        var (iterations, bodyStart) = loopStack.Pop();
        var bodyDuration = accumulatedTime - bodyStart;

        if (iterations > 1 && bodyDuration > TimeSpan.Zero)
            return Result.Ok(accumulatedTime + TimeSpan.FromTicks(bodyDuration.Ticks * (iterations - 1)));

        return Result.Ok(accumulatedTime);
    }

    private static Result<TimeSpan> ProcessRegularStep(Step step, TimeSpan accumulatedTime, int stepIndex)
    {
        var stepDurationResult = GetStepDuration(step, stepIndex);
        if (stepDurationResult.IsFailed)
            return stepDurationResult;

        var stepDuration = stepDurationResult.Value;
        if (stepDuration > TimeSpan.Zero)
            return Result.Ok(accumulatedTime + stepDuration);

        return Result.Ok(accumulatedTime);
    }

    private static Result<TimeSpan> GetStepDuration(Step step, int stepIndex)
    {
        if (step.DeployDuration != DeployDuration.LongLasting)
            return Result.Ok(TimeSpan.Zero);

        if (!step.Properties.TryGetValue(MandatoryColumns.StepDuration, out var durationProperty) || durationProperty == null)
            return Result.Ok(TimeSpan.Zero);

        var seconds = durationProperty.GetValue<float>();

        if (seconds < 0f)
            return CreateNegativeStepDurationError(stepIndex, seconds);

        return Result.Ok(seconds > 0f ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero);
    }

    private static Result CreateMissingIterationCountError(int stepIndex)
    {
        var error = new Error("ForLoop step is missing iteration count.")
            .WithMetadata("code", ErrorCode.CoreForLoopFailure)
            .WithMetadata("stepIndex", stepIndex);
        return Result.Fail(error);
    }

    private static Result CreateInvalidIterationCountError(int stepIndex, float iterations)
    {
        var error = new Error($"Invalid iteration count: {iterations}.")
            .WithMetadata("code", ErrorCode.CoreForLoopFailure)
            .WithMetadata("stepIndex", stepIndex)
            .WithMetadata("iterations", iterations);
        return Result.Fail(error);
    }

    private static Result<TimeSpan> CreateUnmatchedEndForLoopError(int stepIndex)
    {
        var error = new Error("Unmatched 'EndForLoop' found.")
            .WithMetadata("code", ErrorCode.CoreForLoopFailure)
            .WithMetadata("stepIndex", stepIndex);
        return Result.Fail(error);
    }

    private static Result<(TimeSpan, IReadOnlyDictionary<int, TimeSpan>)> CreateUnmatchedForLoopError(int stepIndex)
    {
        var error = new Error("Unmatched 'ForLoop' found.")
            .WithMetadata("code", ErrorCode.CoreForLoopFailure)
            .WithMetadata("stepIndex", stepIndex);
        return Result.Fail(error);
    }


    private static Result<TimeSpan> CreateNegativeStepDurationError(int stepIndex, float seconds)
    {
        var error = new Error($"Negative step duration: {seconds}.")
            .WithMetadata("code", ErrorCode.CoreInvalidStepDuration)
            .WithMetadata("stepIndex", stepIndex)
            .WithMetadata("duration", seconds);
        return Result.Fail(error);
    }
}
