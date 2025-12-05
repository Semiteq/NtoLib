using System;

using FluentResults;

using NtoLib.MbeTable.ModuleCore.Entities;
using NtoLib.MbeTable.ModuleCore.Services;
using NtoLib.MbeTable.ModuleInfrastructure.ActionTarget;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Common;

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
