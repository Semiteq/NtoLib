using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Attributes;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.ActionTartget;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Validation;

/// <summary>
/// Consolidates all recipe validation logic for assembly service.
/// </summary>
public sealed class AssemblyValidator
{
    private readonly RecipeStructureValidator _structureValidator;
    private readonly RecipeLoopValidator _loopValidator;
    private readonly TargetAvailabilityValidator _targetValidator;
    private readonly IActionRepository _actionRepository;
    private readonly IActionTargetProvider _targetProvider;

    public AssemblyValidator(
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

    public Result ValidateRecipe(Recipe recipe)
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

    private Result ValidateStructure(Recipe recipe)
    {
        return _structureValidator.Validate(recipe);
    }

    private Result ValidateLoops(Recipe recipe)
    {
        var result = _loopValidator.Validate(recipe);
        return result.IsSuccess ? Result.Ok() : result.ToResult();
    }

    private Result ValidateTargets(Recipe recipe)
    {
        return _targetValidator.Validate(recipe, _actionRepository, _targetProvider);
    }
}