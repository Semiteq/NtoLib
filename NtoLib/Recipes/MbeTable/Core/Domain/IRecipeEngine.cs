#nullable enable

using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Core.Domain
{
    public interface IRecipeEngine
    {
        Recipe CreateEmptyRecipe();

        Recipe AddDefaultStep(Recipe currentRecipe, int rowIndex);

        Recipe RemoveStep(Recipe currentRecipe, int rowIndex);

        Recipe ReplaceStepWithNewDefault(Recipe currentRecipe, int rowIndex, int newActionId);

        Result<Recipe> UpdateStepProperty(Recipe currentRecipe, int rowIndex, ColumnIdentifier columnKey, object value);
    }
}