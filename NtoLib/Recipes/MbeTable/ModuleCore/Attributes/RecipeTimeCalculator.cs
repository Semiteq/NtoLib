using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Errors;
using NtoLib.Recipes.MbeTable.ModuleCore.Warnings;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Attributes;

public class RecipeTimeCalculator
{
    private const int ForLoopActionId = (int)ServiceActions.ForLoop;
    private const int EndForLoopActionId = (int)ServiceActions.EndForLoop;

    public Result<LoopAnalysisResult> Calculate(Recipe recipe)
    {
        var stepStartTimes = new Dictionary<int, TimeSpan>(recipe.Steps.Count);
        var accumulatedTime = TimeSpan.Zero;
        var loopStack = new Stack<LoopFrame>();
        var result = Result.Ok();

        var loopMetadataByStart = new Dictionary<int, LoopMetadata>();
        var enclosingMapBuilder = new Dictionary<int, List<LoopMetadata>>();

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            stepStartTimes[i] = accumulatedTime;

            var step = recipe.Steps[i];
            var actionIdResult = GetActionId(step);
            if (actionIdResult.IsFailed)
                return actionIdResult.ToResult<LoopAnalysisResult>();

            var actionId = actionIdResult.Value;
            Result stepProcessingResult;

            switch (actionId)
            {
                case ForLoopActionId:
                    stepProcessingResult = ProcessForLoopStart(step, accumulatedTime, loopStack, i);
                    break;

                case EndForLoopActionId:
                    stepProcessingResult = ProcessForLoopEnd(
                        ref accumulatedTime,
                        loopStack,
                        i,
                        loopMetadataByStart,
                        enclosingMapBuilder);
                    break;

                default:
                    stepProcessingResult = ProcessRegularStep(step, ref accumulatedTime, i);
                    break;
            }

            if (stepProcessingResult.IsFailed)
                return stepProcessingResult.ToResult<LoopAnalysisResult>();

            result.WithReasons(stepProcessingResult.Reasons);
        }

        // Do not emit unmatched loop warnings here; Validator is responsible for warnings.
        // Any remaining frames are ignored for timing adjustments (only completed loops add extra iterations).

        var sortedEnclosingMap = BuildEnclosingMap(enclosingMapBuilder);

        var analysis = new LoopAnalysisResult(
            accumulatedTime,
            stepStartTimes.ToImmutableDictionary(),
            loopMetadataByStart.ToImmutableDictionary(),
            sortedEnclosingMap);

        return result.ToResult(analysis);
    }

    private static Result<int> GetActionId(Step step)
    {
        if (!step.Properties.TryGetValue(MandatoryColumns.Action, out var actionProperty) || actionProperty == null)
            return Result.Fail(new CoreStepNoActionPropertyError());

        var getValueResult = actionProperty.GetValue<short>();
        if (getValueResult.IsFailed)
            return getValueResult.ToResult();

        return Result.Ok((int)getValueResult.Value);
    }

    private static Result ProcessForLoopStart(
        Step step,
        TimeSpan accumulatedTime,
        Stack<LoopFrame> loopStack,
        int stepIndex)
    {
        int iterations = 1;

        if (step.Properties.TryGetValue(MandatoryColumns.Task, out var taskProperty) && taskProperty != null)
        {
            var getValueResult = taskProperty.GetValue<float>();
            if (getValueResult.IsSuccess)
            {
                var raw = (int)getValueResult.Value;
                iterations = raw > 0 ? raw : 1;
            }
        }

        loopStack.Push(new LoopFrame
        {
            StartIndex = stepIndex,
            Iterations = iterations,
            LoopStartTime = accumulatedTime,
            BodyStartTime = accumulatedTime
        });

        return Result.Ok();
    }

    private static Result ProcessForLoopEnd(
        ref TimeSpan accumulatedTime,
        Stack<LoopFrame> loopStack,
        int currentStepIndex,
        IDictionary<int, LoopMetadata> loopMetadata,
        IDictionary<int, List<LoopMetadata>> enclosingMapBuilder)
    {
        if (loopStack.Count == 0)
        {
            return Result.Ok();
        }

        var frame = loopStack.Pop();
        var singleIterationDuration = accumulatedTime - frame.BodyStartTime;

        if (singleIterationDuration.Ticks < 0)
            singleIterationDuration = TimeSpan.Zero;

        var metadata = new LoopMetadata(
            frame.StartIndex,
            currentStepIndex,
            loopStack.Count + 1,
            singleIterationDuration,
            frame.Iterations);

        loopMetadata[frame.StartIndex] = metadata;

        AddToEnclosingMap(frame.StartIndex, currentStepIndex, metadata, enclosingMapBuilder);

        var totalLoopTime = TimeSpan.FromTicks(singleIterationDuration.Ticks * (frame.Iterations - 1));
        accumulatedTime += totalLoopTime;

        return Result.Ok();
    }

    private static void AddToEnclosingMap(
        int startIndex,
        int endIndex,
        LoopMetadata metadata,
        IDictionary<int, List<LoopMetadata>> enclosingMapBuilder)
    {
        for (int i = startIndex + 1; i < endIndex; i++)
        {
            if (!enclosingMapBuilder.TryGetValue(i, out var list))
            {
                list = new List<LoopMetadata>();
                enclosingMapBuilder[i] = list;
            }

            list.Add(metadata);
        }
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

        if (!step.Properties.TryGetValue(MandatoryColumns.StepDuration, out var durationProperty) ||
            durationProperty == null)
            return Result.Ok(TimeSpan.Zero);

        var getValueResult = durationProperty.GetValue<float>();
        if (getValueResult.IsFailed)
            return getValueResult.ToResult<TimeSpan>();

        var seconds = getValueResult.Value;
        if (seconds < 0f)
        {
            var result = Result.Ok(TimeSpan.Zero);
            result.WithReason(new CoreStepDurationNegativeWarning(stepIndex, seconds));
            return result;
        }

        return Result.Ok(seconds > 0f ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero);
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<LoopMetadata>> BuildEnclosingMap(
        Dictionary<int, List<LoopMetadata>> enclosingMapBuilder)
    {
        return enclosingMapBuilder.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<LoopMetadata>)kvp.Value
                .OrderByDescending(m => m.NestingDepth)
                .ToList()
                .AsReadOnly());
    }
}