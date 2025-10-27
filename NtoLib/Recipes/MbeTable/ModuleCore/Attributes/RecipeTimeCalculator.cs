using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Attributes;

/// <summary>
/// Calculates total execution time and per-step absolute start times.
/// Supports nested For/EndFor loops, LongLasting/Immediate steps,
/// loop iteration count from MandatoryColumns.Task (short),
/// and step duration from MandatoryColumns.StepDuration (float, seconds).
/// </summary>
public class RecipeTimeCalculator
{
    private const int ForLoopActionId = (int)ServiceActions.ForLoop;
    private const int EndForLoopActionId = (int)ServiceActions.EndForLoop;

    public Result<(TimeSpan, IReadOnlyDictionary<int, TimeSpan>)> Calculate(Recipe recipe)
    {
        var stepStartTimes = new Dictionary<int, TimeSpan>(recipe.Steps.Count);
        var accumulatedTime = TimeSpan.Zero;
        var loopStack = new Stack<LoopFrame>();

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            stepStartTimes[i] = accumulatedTime;

            var step = recipe.Steps[i];
            var actionPropertyResult = GetActionPropertyIfExistsInStep(step, i);
            if (actionPropertyResult.IsFailed)
                return actionPropertyResult.ToResult<(TimeSpan, IReadOnlyDictionary<int, TimeSpan>)>();

            var actionId = actionPropertyResult.Value;

            switch (actionId)
            {
                case ForLoopActionId:
                {
                    var processResult = ProcessForLoopStart(step, accumulatedTime, loopStack, i);
                    if (processResult.IsFailed)
                        return processResult.ToResult<(TimeSpan, IReadOnlyDictionary<int, TimeSpan>)>();
                    break;
                }

                case EndForLoopActionId:
                {
                    var processResult = ProcessForLoopEnd(ref accumulatedTime, loopStack, i);
                    if (processResult.IsFailed)
                        return processResult.ToResult<(TimeSpan, IReadOnlyDictionary<int, TimeSpan>)>();
                    break;
                }

                default:
                {
                    var processResult = ProcessRegularStep(step, ref accumulatedTime, i);
                    if (processResult.IsFailed)
                        return processResult.ToResult<(TimeSpan, IReadOnlyDictionary<int, TimeSpan>)>();
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
                .WithMetadata(nameof(Codes), Codes.CoreActionNotFound)
                .WithMetadata("stepIndex", stepIndex);
            return Result.Fail(error);
        }

        return Result.Ok((int)actionProperty.GetValue<short>());
    }

    private static Result ProcessForLoopStart(
        Step step,
        TimeSpan accumulatedTime,
        Stack<LoopFrame> loopStack,
        int stepIndex)
    {
        if (!step.Properties.TryGetValue(MandatoryColumns.Task, out var taskProperty) || taskProperty == null)
            return CreateMissingIterationCountWarning(stepIndex);

        var iterations = (int)taskProperty.GetValue<short>();
        if (iterations <= 0)
            return CreateInvalidIterationCountError(stepIndex, iterations);

        loopStack.Push(new LoopFrame
        {
            Iterations = iterations,
            LoopStartTime = accumulatedTime,
            BodyStartTime = accumulatedTime
        });

        return Result.Ok();
    }

    private static Result ProcessForLoopEnd(
        ref TimeSpan accumulatedTime,
        Stack<LoopFrame> loopStack,
        int stepIndex)
    {
        if (loopStack.Count == 0)
            return CreateUnmatchedEndForLoopWarning(stepIndex);

        var frame = loopStack.Pop();

        var singleIterationDuration = accumulatedTime - frame.BodyStartTime;
        var totalLoopTime = TimeSpan.FromTicks(singleIterationDuration.Ticks * frame.Iterations);
        accumulatedTime = frame.LoopStartTime + totalLoopTime;

        return Result.Ok();
    }

    private static Result ProcessRegularStep(Step step, ref TimeSpan accumulatedTime, int stepIndex)
    {
        var stepDurationResult = GetStepDuration(step, stepIndex);
        if (stepDurationResult.IsFailed)
            return stepDurationResult.ToResult();

        var stepDuration = stepDurationResult.Value;
        if (stepDuration > TimeSpan.Zero)
            accumulatedTime += stepDuration;

        return Result.Ok();
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

    private static Result CreateMissingIterationCountWarning(int stepIndex)
    {
        return Result.Ok()
            .WithReason(new ValidationIssue(Codes.CoreForLoopError).WithMetadata("stepIndex", stepIndex));
    }

    private static Result CreateInvalidIterationCountError(int stepIndex, int iterations)
    {
        var error = new Error($"Invalid iteration count: {iterations}.")
            .WithMetadata(nameof(Codes), Codes.CoreInvalidStepDuration)
            .WithMetadata("stepIndex", stepIndex)
            .WithMetadata("iterations", iterations);
        return Result.Fail(error);
    }

    private static Result CreateUnmatchedEndForLoopWarning(int stepIndex)
    {
        return Result.Ok()
            .WithReason(new ValidationIssue(Codes.CoreForLoopError).WithMetadata("stepIndex", stepIndex));
    }

    private static Result<(TimeSpan, IReadOnlyDictionary<int, TimeSpan>)> CreateUnmatchedForLoopError(int stepIndex)
    {
        return Result.Ok((TimeSpan.Zero, (IReadOnlyDictionary<int, TimeSpan>)new Dictionary<int, TimeSpan>()))
            .WithReason(new ValidationIssue(Codes.CoreForLoopError).WithMetadata("stepIndex", stepIndex));
    }

    private static Result<TimeSpan> CreateNegativeStepDurationError(int stepIndex, float seconds)
    {
        var error = new Error($"Negative step duration: {seconds}.")
            .WithMetadata(nameof(Codes), Codes.CoreInvalidStepDuration)
            .WithMetadata("stepIndex", stepIndex)
            .WithMetadata("duration", seconds);
        return Result.Fail(error);
    }

    private sealed class LoopFrame
    {
        public int Iterations { get; set; }
        public TimeSpan LoopStartTime { get; set; }
        public TimeSpan BodyStartTime { get; set; }
    }
}