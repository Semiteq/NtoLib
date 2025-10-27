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
        var result = Result.Ok();

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            stepStartTimes[i] = accumulatedTime;

            var step = recipe.Steps[i];
            var actionPropertyResult = GetActionPropertyIfExistsInStep(step, i);
            if (actionPropertyResult.IsFailed)
                return actionPropertyResult.ToResult<(TimeSpan, IReadOnlyDictionary<int, TimeSpan>)>();

            var actionId = actionPropertyResult.Value;
            Result stepProcessingResult;

            switch (actionId)
            {
                case ForLoopActionId:
                    stepProcessingResult = ProcessForLoopStart(step, accumulatedTime, loopStack, i);
                    break;
                
                case EndForLoopActionId:
                    stepProcessingResult = ProcessForLoopEnd(ref accumulatedTime, loopStack, i);
                    break;

                default:
                    stepProcessingResult = ProcessRegularStep(step, ref accumulatedTime, i);
                    break;
            }

            if (stepProcessingResult.IsFailed)
                return stepProcessingResult.ToResult<(TimeSpan, IReadOnlyDictionary<int, TimeSpan>)>();
            
            result.WithReasons(stepProcessingResult.Reasons);
        }

        if (loopStack.Count > 0)
        {
            result.WithReason(new ValidationIssue(Codes.CoreForLoopError).WithMetadata("stepIndex", recipe.Steps.Count));
        }

        return result.ToResult((accumulatedTime, (IReadOnlyDictionary<int, TimeSpan>)stepStartTimes.ToImmutableDictionary()));
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
            return CreateInvalidIterationCountWarning(stepIndex, iterations);

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
        
        // If duration is negative, it indicates an issue with loop structure that should have been caught earlier.
        // To prevent catastrophic time calculation errors, we treat this iteration as having zero duration.
        if (singleIterationDuration.Ticks < 0)
            singleIterationDuration = TimeSpan.Zero;
            
        var totalLoopTime = TimeSpan.FromTicks(singleIterationDuration.Ticks * (frame.Iterations - 1));
        accumulatedTime += totalLoopTime;

        return Result.Ok();
    }

    private static Result ProcessRegularStep(Step step, ref TimeSpan accumulatedTime, int stepIndex)
    {
        var result = Result.Ok();
        var stepDurationResult = GetStepDuration(step, stepIndex);
        if (stepDurationResult.IsFailed)
            return stepDurationResult.ToResult();

        result.WithReasons(stepDurationResult.Reasons);

        var stepDuration = stepDurationResult.Value;
        if (stepDuration > TimeSpan.Zero)
            accumulatedTime += stepDuration;

        return result;
    }

    private static Result<TimeSpan> GetStepDuration(Step step, int stepIndex)
    {
        if (step.DeployDuration != DeployDuration.LongLasting)
            return Result.Ok(TimeSpan.Zero);

        if (!step.Properties.TryGetValue(MandatoryColumns.StepDuration, out var durationProperty) || durationProperty == null)
            return Result.Ok(TimeSpan.Zero);

        var seconds = durationProperty.GetValue<float>();
        if (seconds < 0f)
            return CreateNegativeStepDurationWarning(stepIndex, seconds);

        return Result.Ok(seconds > 0f ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero);
    }

    private static Result CreateMissingIterationCountWarning(int stepIndex)
    {
        return Result.Ok()
            .WithReason(new ValidationIssue(Codes.CoreForLoopError)
            .WithMetadata("stepIndex", stepIndex)
            .WithMetadata("details", "Missing iteration count property."));
    }

    private static Result CreateInvalidIterationCountWarning(int stepIndex, int iterations)
    {
        return Result.Ok()
            .WithReason(new ValidationIssue(Codes.CoreForLoopError)
            .WithMetadata("stepIndex", stepIndex)
            .WithMetadata("iterations", iterations)
            .WithMetadata("details", $"Invalid iteration count: {iterations}."));
    }

    private static Result CreateUnmatchedEndForLoopWarning(int stepIndex)
    {
        return Result.Ok()
            .WithReason(new ValidationIssue(Codes.CoreForLoopError)
            .WithMetadata("stepIndex", stepIndex)
            .WithMetadata("details", "Unmatched EndFor loop."));
    }

    private static Result<TimeSpan> CreateNegativeStepDurationWarning(int stepIndex, float seconds)
    {
        var result = Result.Ok(TimeSpan.Zero);
        result.WithReason(new ValidationIssue(Codes.CoreInvalidStepDuration)
            .WithMetadata("stepIndex", stepIndex)
            .WithMetadata("duration", seconds));
        return result;
    }

    private sealed class LoopFrame
    {
        public int Iterations { get; set; }
        public TimeSpan LoopStartTime { get; set; }
        public TimeSpan BodyStartTime { get; set; }
    }
}