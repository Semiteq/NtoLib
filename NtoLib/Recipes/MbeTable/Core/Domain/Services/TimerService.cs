using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Services;

public class TimerService
{
    private readonly IPlcStateMonitor _plcStateMonitor;

    private TimeSpan _totalRecipeDuration;
    private IReadOnlyDictionary<int, TimeSpan> _stepStartTimes;
    private bool _hasData;
    
    public event Action<TimeSpan, TimeSpan>? TimesUpdated;

    public TimerService(IPlcStateMonitor plcStateMonitor)
    {
        _plcStateMonitor = plcStateMonitor;
        _stepStartTimes = new Dictionary<int, TimeSpan>();
    }
    
    public void SetTimeAnalysisData(RecipeTimeAnalysis? recipeTimeAnalysis)
    {
        if (recipeTimeAnalysis == null)
        {
            _hasData = false;
            _totalRecipeDuration = TimeSpan.Zero;
            _stepStartTimes = new Dictionary<int, TimeSpan>();
            return;
        }

        _totalRecipeDuration = recipeTimeAnalysis.TotalDuration;
        _stepStartTimes = recipeTimeAnalysis.StepStartTimes;
        _hasData = _stepStartTimes.Any();
    }
    
    public void Update()
    {
        if (!_hasData)
        {
            TimesUpdated?.Invoke(TimeSpan.Zero, TimeSpan.Zero);
            return;
        }

        var currentLine = _plcStateMonitor.CurrentLineNumber;
        var elapsedStepTimeSeconds = _plcStateMonitor.StepCurrentTime;

        if (currentLine < 0 || currentLine >= _stepStartTimes.Count)
        {
            TimesUpdated?.Invoke(TimeSpan.Zero, TimeSpan.Zero);
            return;
        }

        var elapsedStepTime = TimeSpan.FromSeconds(elapsedStepTimeSeconds);

        var stepStartTime = _stepStartTimes[currentLine];
        var nextStepStartTime = (currentLine + 1 < _stepStartTimes.Count)
            ? _stepStartTimes[currentLine + 1]
            : _totalRecipeDuration;
        var stepDuration = nextStepStartTime - stepStartTime;
        var stepTimeLeft = stepDuration > elapsedStepTime ? stepDuration - elapsedStepTime : TimeSpan.Zero;

        var totalTimeElapsed = stepStartTime + elapsedStepTime;
        var totalTimeLeft = _totalRecipeDuration > totalTimeElapsed ? _totalRecipeDuration - totalTimeElapsed : TimeSpan.Zero;

        TimesUpdated?.Invoke(stepTimeLeft, totalTimeLeft);
    }
}