using System;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Attributes;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.ActionTartget;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Validation;

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
        var result = Result.Ok();
        
        var structureResult = _structureValidator.Validate(recipe);
        if (structureResult.IsFailed)
            return structureResult;
        
        var loopResult = _loopValidator.Validate(recipe);
        if (loopResult.IsFailed)
            return loopResult.ToResult();
        
        if (loopResult.Reasons.Count > 0)
            result = result.WithReasons(loopResult.Reasons);
        
        var targetResult = _targetValidator.Validate(recipe, _actionRepository, _targetProvider);
        if (targetResult.IsFailed)
            return targetResult;
        
        return result;
    }
}