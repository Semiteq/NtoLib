using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Errors;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Attributes;

public sealed class RecipeAttributesService : IRecipeAttributesService
{
    private readonly RecipeStructureValidator _structureValidator;
    private readonly RecipeLoopValidator _loopValidator;
    private readonly RecipeTimeCalculator _timeCalculator;

    private readonly IReadOnlyList<LoopMetadata> _emptyLoopList = Array.Empty<LoopMetadata>();

    private IReadOnlyDictionary<int, int> _loopNestingLevels;
    private IReadOnlyDictionary<int, TimeSpan> _stepStartTimes;
    private IReadOnlyDictionary<int, IReadOnlyList<LoopMetadata>> _enclosingLoopsMap;
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

        _loopNestingLevels = ImmutableDictionary<int, int>.Empty;
        _stepStartTimes = ImmutableDictionary<int, TimeSpan>.Empty;
        _enclosingLoopsMap = ImmutableDictionary<int, IReadOnlyList<LoopMetadata>>.Empty;
        _totalDuration = TimeSpan.Zero;
        _isValid = false;
    }

    public Result UpdateAttributes(Recipe recipe)
    {
        var previousValidState = _isValid;

        var structureResult = _structureValidator.Validate(recipe);
        if (structureResult.IsFailed)
        {
            ResetToInvalidState(previousValidState);
            return structureResult;
        }

        var loopResult = _loopValidator.Validate(recipe);
        if (loopResult.IsFailed)
        {
            ResetToInvalidState(previousValidState);
            return loopResult.ToResult();
        }

        _loopNestingLevels = loopResult.Value;

        var timeResult = _timeCalculator.Calculate(recipe);
        if (timeResult.IsFailed)
        {
            ResetToInvalidState(previousValidState);
            return timeResult.ToResult();
        }

        var analysis = timeResult.Value;
        _totalDuration = analysis.TotalDuration;
        _stepStartTimes = analysis.StepStartTimes;
        _enclosingLoopsMap = analysis.EnclosingLoopsMap;

        var allReasons = loopResult.Reasons.Concat(timeResult.Reasons).ToList();

        _isValid = true;

        NotifyValidationStateChanged(previousValidState);

        var finalResult = Result.Ok();
        if (allReasons.Any())
            finalResult.WithReasons(allReasons);

        return finalResult;
    }

    public Result<int> GetLoopNestingLevel(int stepIndex)
    {
        return _loopNestingLevels.TryGetValue(stepIndex, out var level)
            ? Result.Ok(level)
            : new CoreIndexOutOfRangeError(stepIndex, _loopNestingLevels.Count);
    }

    public Result<TimeSpan> GetStepStartTime(int stepIndex)
    {
        return _stepStartTimes.TryGetValue(stepIndex, out var time)
            ? Result.Ok(time)
            : new CoreIndexOutOfRangeError(stepIndex, _stepStartTimes.Count);
    }

    public IReadOnlyList<LoopMetadata> GetEnclosingLoops(int stepIndex)
    {
        return _enclosingLoopsMap.TryGetValue(stepIndex, out var loops)
            ? loops
            : _emptyLoopList;
    }

    public IReadOnlyDictionary<int, TimeSpan> GetAllStepStartTimes() => _stepStartTimes;

    public TimeSpan GetTotalDuration() => _totalDuration;

    public bool IsValid() => _isValid;

    private void ResetToInvalidState(bool previousState)
    {
        _isValid = false;
        _loopNestingLevels = ImmutableDictionary<int, int>.Empty;
        _stepStartTimes = ImmutableDictionary<int, TimeSpan>.Empty;
        _enclosingLoopsMap = ImmutableDictionary<int, IReadOnlyList<LoopMetadata>>.Empty;
        _totalDuration = TimeSpan.Zero;
        NotifyValidationStateChanged(previousState);
    }

    private void NotifyValidationStateChanged(bool previousState)
    {
        if (previousState != _isValid)
            ValidationStateChanged?.Invoke(_isValid);
    }
}