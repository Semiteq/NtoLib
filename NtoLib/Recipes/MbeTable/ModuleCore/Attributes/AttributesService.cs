using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Attributes;

/// <summary>
/// Manages Recipe analysis results including structure validation, loop validation, and time calculation.
/// Stores computed attributes and provides query API for accessing them.
/// </summary>
public sealed class RecipeAttributesService : IRecipeAttributesService
{
    private readonly RecipeStructureValidator _structureValidator;
    private readonly RecipeLoopValidator _loopValidator;
    private readonly RecipeTimeCalculator _timeCalculator;

    private IReadOnlyDictionary<int, int> _loopNestingLevels;
    private IReadOnlyDictionary<int, TimeSpan> _stepStartTimes;
    private TimeSpan _totalDuration;
    private bool _isValid;

    public event Action<bool>? ValidationStateChanged;

    public RecipeAttributesService(
        RecipeStructureValidator structureValidator,
        RecipeLoopValidator loopValidator,
        RecipeTimeCalculator timeCalculator)
    {
        _structureValidator = structureValidator ?? throw new ArgumentNullException(nameof(structureValidator));
        _loopValidator = loopValidator ?? throw new ArgumentNullException(nameof(loopValidator));
        _timeCalculator = timeCalculator ?? throw new ArgumentNullException(nameof(timeCalculator));

        _loopNestingLevels = new Dictionary<int, int>();
        _stepStartTimes = new Dictionary<int, TimeSpan>();
        _totalDuration = TimeSpan.Zero;
        _isValid = false;
    }

    public Result UpdateAttributes(Recipe recipe)
    {
        var previousValidState = _isValid;

        var structureResult = _structureValidator.Validate(recipe);
        if (structureResult.IsFailed)
        {
            ResetToInvalidState();
            NotifyValidationStateChanged(previousValidState);
            return structureResult;
        }

        var loopResult = _loopValidator.Validate(recipe);
        if (loopResult.IsFailed)
        {
            ResetToInvalidState();
            NotifyValidationStateChanged(previousValidState);
            return loopResult.ToResult();
        }

        _loopNestingLevels = loopResult.Value;

        var timeResult = _timeCalculator.Calculate(recipe);
        if (timeResult.IsFailed)
        {
            ResetToInvalidState();
            NotifyValidationStateChanged(previousValidState);
            return timeResult.ToResult();
        }

        _totalDuration = timeResult.Value.Item1;
        _stepStartTimes = timeResult.Value.Item2;
        _isValid = true;

        NotifyValidationStateChanged(previousValidState);
        return Result.Ok();
    }

    public Result<int> GetLoopNestingLevel(int stepIndex)
    {
        if (_loopNestingLevels.TryGetValue(stepIndex, out var level))
            return Result.Ok(level);

        return Result.Fail(new Error($"No nesting level found for step index {stepIndex}")
            .WithMetadata("code", Codes.CoreIndexOutOfRange)
            .WithMetadata("stepIndex", stepIndex));
    }

    public Result<TimeSpan> GetStepStartTime(int stepIndex)
    {
        if (_stepStartTimes.TryGetValue(stepIndex, out var time))
            return Result.Ok(time);

        return Result.Fail(new Error($"No start time found for step index {stepIndex}")
            .WithMetadata("code", Codes.CoreIndexOutOfRange)
            .WithMetadata("stepIndex", stepIndex));
    }

    public IReadOnlyDictionary<int, TimeSpan> GetAllStepStartTimes() => _stepStartTimes;

    public TimeSpan GetTotalDuration() => _totalDuration;

    public bool IsValid() => _isValid;

    private void ResetToInvalidState()
    {
        _isValid = false;
        _loopNestingLevels = new Dictionary<int, int>();
        _stepStartTimes = new Dictionary<int, TimeSpan>();
        _totalDuration = TimeSpan.Zero;
    }

    private void NotifyValidationStateChanged(bool previousState)
    {
        if (previousState != _isValid)
        {
            ValidationStateChanged?.Invoke(_isValid);
        }
    }
}