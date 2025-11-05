using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Attributes;

public sealed class RecipeStructureValidator
{
    public Result Validate(Recipe recipe)
    {
        if (recipe == null)
            return Errors.RecipeNull();

        if (recipe.Steps == null)
            return Errors.RecipeStepsNull();

        for (int i = 0; i < recipe.Steps.Count; i++)
        {
            var step = recipe.Steps[i];

            if (step == null)
                return Errors.StepNull(i);

            if (!step.Properties.ContainsKey(MandatoryColumns.Action))
                return Errors.StepMissingAction(i);

            var actionProperty = step.Properties[MandatoryColumns.Action];
            if (actionProperty == null)
                return Errors.StepActionNull(i);
        }

        return Result.Ok();
    }
}