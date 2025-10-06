

using System;
using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Attributes;
using NtoLib.Recipes.MbeTable.Core.Entities;
using NtoLib.Recipes.MbeTable.Core.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.ActionTartget;
using NtoLib.Recipes.MbeTable.RecipeAssemblyService.Validation;

namespace NtoLib.Recipes.MbeTable.Csv.Validation;

/// <summary>
/// Consolidates all recipe validation logic.
/// </summary>
public sealed class RecipeValidator : IRecipeValidator
{
    private readonly RecipeStructureValidator _structureValidator;
    private readonly RecipeLoopValidator _loopValidator;
    private readonly TargetAvailabilityValidator _targetValidator;
    private readonly IActionRepository _actionRepository;
    private readonly IActionTargetProvider _targetProvider;

    public RecipeValidator(
        RecipeStructureValidator structureValidator,
        RecipeLoopValidator loopValidator,
        TargetAvailabilityValidator targetValidator,
        IActionRepository actionRepository,
        IActionTargetProvider targetProvider)
    {
        _structureValidator = structureValidator ?? throw new ArgumentNullException(nameof(structureValidator));
        _loopValidator = loopValidator ?? throw new ArgumentNullException(nameof(loopValidator));
        _targetValidator = targetValidator ?? throw new ArgumentNullException(nameof(targetValidator));
        _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
        _targetProvider = targetProvider ?? throw new ArgumentNullException(nameof(targetProvider));
    }

    public Result ValidateStructure(Recipe recipe)
    {
        return _structureValidator.Validate(recipe);
    }

    public Result ValidateLoops(Recipe recipe)
    {
        var result = _loopValidator.Validate(recipe);
        return result.IsSuccess ? Result.Ok() : result.ToResult();
    }

    public Result ValidateTargets(Recipe recipe)
    {
        return _targetValidator.Validate(recipe, _actionRepository, _targetProvider);
    }

    public Result ValidateAll(Recipe recipe)
    {
        var errors = new List<IError>();
        
        var structureResult = ValidateStructure(recipe);
        if (structureResult.IsFailed)
        {
            errors.AddRange(structureResult.Errors);
        }
        
        var loopResult = ValidateLoops(recipe);
        if (loopResult.IsFailed)
        {
            errors.AddRange(loopResult.Errors);
        }
        
        var targetResult = ValidateTargets(recipe);
        if (targetResult.IsFailed)
        {
            errors.AddRange(targetResult.Errors);
        }
        
        return errors.Count > 0 
            ? Result.Fail(errors) 
            : Result.Ok();
    }
}