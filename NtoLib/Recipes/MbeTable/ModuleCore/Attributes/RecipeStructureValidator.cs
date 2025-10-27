using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Attributes;

public sealed class RecipeStructureValidator
{
    public Result Validate(Recipe recipe)
    {
        if (recipe == null)
        {
            return Result.Fail(new Error("Recipe cannot be null")
                .WithMetadata(nameof(Codes), Codes.CoreValidationFailed));
        }

        if (recipe.Steps == null)
        {
            return Result.Fail(new Error("Recipe.Steps cannot be null")
                .WithMetadata(nameof(Codes), Codes.CoreValidationFailed));
        }

        for (int i = 0; i < recipe.Steps.Count; i++)
        {
            var step = recipe.Steps[i];

            if (step == null)
            {
                return Result.Fail(new Error($"Step at index {i} is null")
                    .WithMetadata(nameof(Codes), Codes.CoreValidationFailed)
                    .WithMetadata("stepIndex", i));
            }

            if (!step.Properties.ContainsKey(MandatoryColumns.Action))
            {
                return Result.Fail(new Error($"Step at index {i} is missing Action property")
                    .WithMetadata(nameof(Codes), Codes.CoreActionNotFound)
                    .WithMetadata("stepIndex", i));
            }

            var actionProperty = step.Properties[MandatoryColumns.Action];
            if (actionProperty == null)
            {
                return Result.Fail(new Error($"Step at index {i} has null Action property")
                    .WithMetadata(nameof(Codes), Codes.CoreActionNotFound)
                    .WithMetadata("stepIndex", i));
            }
        }

        return Result.Ok();
    }
}