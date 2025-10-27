using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

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
    private readonly ErrorDefinitionRegistry _errorRegistry;
    private readonly ILogger<RecipeAttributesService> _logger;

    private IReadOnlyDictionary<int, int> _loopNestingLevels;
    private IReadOnlyDictionary<int, TimeSpan> _stepStartTimes;
    private TimeSpan _totalDuration;
    private bool _isValid;

    public event Action<bool>? ValidationStateChanged;

    public RecipeAttributesService(
        RecipeStructureValidator structureValidator,
        RecipeLoopValidator loopValidator,
        RecipeTimeCalculator timeCalculator,
        ErrorDefinitionRegistry errorRegistry,
        ILogger<RecipeAttributesService> logger)
    {
        _structureValidator = structureValidator ?? throw new ArgumentNullException(nameof(structureValidator));
        _loopValidator = loopValidator ?? throw new ArgumentNullException(nameof(loopValidator));
        _timeCalculator = timeCalculator ?? throw new ArgumentNullException(nameof(timeCalculator));
        _errorRegistry = errorRegistry ?? throw new ArgumentNullException(nameof(errorRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
        (_totalDuration, _stepStartTimes) = timeResult.Value;
        
        var allReasons = loopResult.Reasons.Concat(timeResult.Reasons).ToList();

        _isValid = !allReasons
            .Select(reason => reason.TryGetCode(out var code) ? code : Codes.UnknownError)
            .Any(code => _errorRegistry.Blocks(code, BlockingScope.SaveAndSend));

        NotifyValidationStateChanged(previousValidState);

        var finalResult = Result.Ok();
        if (allReasons.Any())
        {
            finalResult.WithReasons(allReasons);
        }

        return finalResult;
    }

    public Result<int> GetLoopNestingLevel(int stepIndex)
    {
        if (_loopNestingLevels.TryGetValue(stepIndex, out var level))
            return Result.Ok(level);

        return Result.Fail(new Error($"No nesting level found for step index {stepIndex}")
            .WithMetadata(nameof(Codes), Codes.CoreIndexOutOfRange)
            .WithMetadata("stepIndex", stepIndex));
    }

    public Result<TimeSpan> GetStepStartTime(int stepIndex)
    {
        if (_stepStartTimes.TryGetValue(stepIndex, out var time))
            return Result.Ok(time);

        return Result.Fail(new Error($"No start time found for step index {stepIndex}")
            .WithMetadata(nameof(Codes), Codes.CoreIndexOutOfRange)
            .WithMetadata("stepIndex", stepIndex));
    }

    public IReadOnlyDictionary<int, TimeSpan> GetAllStepStartTimes() => _stepStartTimes;

    public TimeSpan GetTotalDuration() => _totalDuration;

    public bool IsValid() => _isValid;

    private void ResetToInvalidState(bool previousState)
    {
        _isValid = false;
        _loopNestingLevels = new Dictionary<int, int>();
        _stepStartTimes = new Dictionary<int, TimeSpan>();
        _totalDuration = TimeSpan.Zero;
        NotifyValidationStateChanged(previousState);
    }

    private void NotifyValidationStateChanged(bool previousState)
    {
        if (previousState != _isValid)
        {
            ValidationStateChanged?.Invoke(_isValid);
        }
    }
}