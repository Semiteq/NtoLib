using System;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.ActionTarget;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Common;

/// <summary>
/// Validates assembled recipes against environment constraints (targets availability).
/// Domain validation is performed by Core analyzer after loading the recipe.
/// </summary>
public sealed class AssemblyValidator
{
	private readonly TargetAvailabilityValidator _targetValidator;
	private readonly IActionRepository _actionRepository;
	private readonly IActionTargetProvider _targetProvider;

	public AssemblyValidator(
		TargetAvailabilityValidator targetValidator,
		IActionRepository actionRepository,
		IActionTargetProvider targetProvider)
	{
		_targetValidator = targetValidator ?? throw new ArgumentNullException(nameof(targetValidator));
		_actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
		_targetProvider = targetProvider ?? throw new ArgumentNullException(nameof(targetProvider));
	}

	public Result ValidateRecipe(Recipe recipe)
	{
		return _targetValidator.Validate(recipe, _actionRepository, _targetProvider);
	}
}
