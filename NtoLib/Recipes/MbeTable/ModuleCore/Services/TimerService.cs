using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Services;

/// <summary>
/// Computes remaining time for current step and whole recipe based on
/// recipe time analysis and current runtime snapshot (PLC-driven elapsed time).
/// </summary>
public sealed class TimerService : ITimerControl
{
    private readonly IRecipeService _recipeService;
    private readonly ILogger<TimerService> _logger;

    private TimeSpan _lastTotalElapsed = TimeSpan.Zero;
    private volatile bool _staticMode = true;

    public event Action<TimeSpan, TimeSpan>? TimesUpdated;

    public TimerService(IRecipeService recipeService, ILogger<TimerService> logger)
    {
        _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void ResetForNewRecipe()
    {
        _staticMode = true;
        _lastTotalElapsed = TimeSpan.Zero;
    }

    public void UpdateFromSnapshot(RecipeRuntimeSnapshot snapshot)
    {
        var stepStartTimes = _recipeService.GetAllStepStartTimes();
        var totalDuration = _recipeService.GetTotalDuration();

        if (!stepStartTimes.Any())
        {
            TimesUpdated?.Invoke(TimeSpan.Zero, TimeSpan.Zero);
            return;
        }

        // While recipe is not active show static totals and ignore PLC counters/elapsed
        if (!snapshot.RecipeActive)
        {
            _staticMode = true;
            _lastTotalElapsed = TimeSpan.Zero;
            TimesUpdated?.Invoke(TimeSpan.Zero, totalDuration);
            return;
        }

        // Transition from static to active mode
        if (_staticMode && snapshot.RecipeActive)
        {
            _staticMode = false;
            _lastTotalElapsed = TimeSpan.Zero;
        }

        if (snapshot.StepIndex < 0 || snapshot.StepIndex >= stepStartTimes.Count)
        {
            TimesUpdated?.Invoke(TimeSpan.Zero, totalDuration);
            return;
        }

        var baseStepStartTime = stepStartTimes[snapshot.StepIndex];
        var elapsedInStep = TimeSpan.FromSeconds(snapshot.StepElapsedSeconds);

        var loopOffset = CalculateLoopOffset(snapshot);
        var totalElapsed = baseStepStartTime + loopOffset + elapsedInStep;

        // Enforce monotonic time to prevent total elapsed time from going backwards
        if (totalElapsed < _lastTotalElapsed)
        {
            _logger.LogWarning(
                "PLC time regression detected. Calculated elapsed: {Calculated}, Last known: {LastKnown}. Clamping to last known value.",
                totalElapsed, _lastTotalElapsed);
            totalElapsed = _lastTotalElapsed;
        }
        _lastTotalElapsed = totalElapsed;

        var totalTimeLeft = totalDuration > totalElapsed ? totalDuration - totalElapsed : TimeSpan.Zero;

        var nextStepStartResult = _recipeService.GetStepStartTime(snapshot.StepIndex + 1);
        var nextStepStart = nextStepStartResult.IsSuccess ? nextStepStartResult.Value : totalDuration;
        var stepDuration = nextStepStart - baseStepStartTime;
        var stepTimeLeft = stepDuration > elapsedInStep ? stepDuration - elapsedInStep : TimeSpan.Zero;

        TimesUpdated?.Invoke(stepTimeLeft, totalTimeLeft);
    }

    private TimeSpan CalculateLoopOffset(RecipeRuntimeSnapshot snapshot)
    {
        var enclosingLoops = _recipeService.GetEnclosingLoops(snapshot.StepIndex);
        if (enclosingLoops.Count == 0)
        {
            return TimeSpan.Zero;
        }

        var offset = TimeSpan.Zero;
        foreach (var loop in enclosingLoops)
        {
            var completedIterations = GetCompletedIterations(snapshot, loop.NestingDepth);

            if (completedIterations >= loop.IterationCount && loop.IterationCount > 0)
            {
                _logger.LogWarning(
                    "PLC counter for loop at depth {Depth} ({PLCCount}) exceeds configured iteration count ({ConfigCount}). Clamping value.",
                    loop.NestingDepth, completedIterations, loop.IterationCount);
                completedIterations = loop.IterationCount - 1;
            }

            offset += TimeSpan.FromTicks(loop.IterationDuration.Ticks * completedIterations);
        }

        return offset;
    }

    private int GetCompletedIterations(RecipeRuntimeSnapshot snapshot, int nestingDepth)
    {
        return nestingDepth switch
        {
            1 => snapshot.ForLevel1Count,
            2 => snapshot.ForLevel2Count,
            3 => snapshot.ForLevel3Count,
            _ => 0
        };
    }
}