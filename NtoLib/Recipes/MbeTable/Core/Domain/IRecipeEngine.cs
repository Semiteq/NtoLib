#nullable enable

using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;

namespace NtoLib.Recipes.MbeTable.Core.Domain;

public interface IRecipeEngine
{
    public Recipe CreateEmptyRecipe();

    public Recipe AddDefaultStep(Recipe currentRecipe, int rowIndex);
    
    public Recipe RemoveStep(Recipe currentRecipe, int rowIndex);
    
    public Recipe ReplaceStepWithNewDefault(Recipe currentRecipe, int rowIndex, int newActionId);

    public (Recipe NewRecipe, RecipePropertyError? Error) UpdateStepProperty(Recipe currentRecipe, int rowIndex, ColumnIdentifier columnKey, object value);
}