

using System;
using System.Linq;
using NtoLib.Recipes.MbeTable.Core.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Core.Services;

/// <summary>
/// Computes remaining time for current step and whole recipe based on
/// recipe time analysis and current runtime snapshot (PLC-driven elapsed time).
/// </summary>
public sealed class TimerService
{
    private readonly IRecipeService _recipeService;

    public event Action<TimeSpan, TimeSpan>? TimesUpdated;

    public TimerService(IRecipeService recipeService)
    {
        _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
    }

    public void UpdateFromSnapshot(RecipeRuntimeSnapshot snapshot)
    {
        var stepStartTimes = _recipeService.GetAllStepStartTimes();
        
        if (!stepStartTimes.Any())
        {
            TimesUpdated?.Invoke(TimeSpan.Zero, TimeSpan.Zero);
            return;
        }

        if (snapshot.StepIndex < 0 || snapshot.StepIndex >= stepStartTimes.Count)
        {
            TimesUpdated?.Invoke(TimeSpan.Zero, TimeSpan.Zero);
            return;
        }

        var elapsed = TimeSpan.FromSeconds(snapshot.StepElapsedSeconds);
        var totalDuration = _recipeService.GetTotalDuration();

        var stepStartResult = _recipeService.GetStepStartTime(snapshot.StepIndex);
        if (stepStartResult.IsFailed)
        {
            TimesUpdated?.Invoke(TimeSpan.Zero, TimeSpan.Zero);
            return;
        }

        var stepStart = stepStartResult.Value;
        var nextStepStartResult = _recipeService.GetStepStartTime(snapshot.StepIndex + 1);
        var nextStepStart = nextStepStartResult.IsSuccess ? nextStepStartResult.Value : totalDuration;

        var stepDuration = nextStepStart - stepStart;
        var stepTimeLeft = stepDuration > elapsed ? stepDuration - elapsed : TimeSpan.Zero;

        var totalElapsed = stepStart + elapsed;
        var totalTimeLeft = totalDuration > totalElapsed ? totalDuration - totalElapsed : TimeSpan.Zero;

        TimesUpdated?.Invoke(stepTimeLeft, totalTimeLeft);
    }
}