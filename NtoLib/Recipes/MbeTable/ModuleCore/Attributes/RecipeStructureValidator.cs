using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Errors;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Attributes;

public sealed class RecipeStructureValidator
{
    public Result Validate(Recipe recipe)
    {
        if (recipe.Steps.Count == 0)
            return Result.Ok();

        for (int i = 0; i < recipe.Steps.Count; i++)
        {
            var step = recipe.Steps[i];

            if (step == null)
                return new CoreStepNullError(i);

            if (!step.Properties.ContainsKey(MandatoryColumns.Action))
                return new CoreStepMissingActionError(i);

            var actionProperty = step.Properties[MandatoryColumns.Action];
            if (actionProperty == null)
                return new CoreStepActionNullError(i);
        }

        return Result.Ok();
    }
}