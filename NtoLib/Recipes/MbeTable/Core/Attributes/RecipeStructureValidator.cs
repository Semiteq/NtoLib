

using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Core.Entities;
using NtoLib.Recipes.MbeTable.Journaling.Errors;

namespace NtoLib.Recipes.MbeTable.Core.Attributes;

/// <summary>
/// Validates basic Recipe structure invariants.
/// </summary>
public sealed class RecipeStructureValidator
{
    public Result Validate(Recipe recipe)
    {
        if (recipe == null)
        {
            return Result.Fail(new Error("Recipe cannot be null")
                .WithMetadata("code", ErrorCode.BusinessInvariantViolation));
        }

        if (recipe.Steps == null)
        {
            return Result.Fail(new Error("Recipe.Steps cannot be null")
                .WithMetadata("code", ErrorCode.BusinessInvariantViolation));
        }

        for (int i = 0; i < recipe.Steps.Count; i++)
        {
            var step = recipe.Steps[i];
            
            if (step == null)
            {
                return Result.Fail(new Error($"Step at index {i} is null")
                    .WithMetadata("code", ErrorCode.BusinessInvariantViolation)
                    .WithMetadata("stepIndex", i));
            }

            if (!step.Properties.ContainsKey(MandatoryColumns.Action))
            {
                return Result.Fail(new Error($"Step at index {i} is missing Action property")
                    .WithMetadata("code", ErrorCode.CoreNoActionFound)
                    .WithMetadata("stepIndex", i));
            }

            var actionProperty = step.Properties[MandatoryColumns.Action];
            if (actionProperty == null)
            {
                return Result.Fail(new Error($"Step at index {i} has null Action property")
                    .WithMetadata("code", ErrorCode.CoreNoActionFound)
                    .WithMetadata("stepIndex", i));
            }
        }

        return Result.Ok();
    }
}