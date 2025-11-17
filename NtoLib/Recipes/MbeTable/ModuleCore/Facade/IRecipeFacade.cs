using FluentResults;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Snapshot;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Facade;

/// <summary>
/// High-level mutation API producing analysis snapshots.
/// </summary>
public interface IRecipeFacade
{
    RecipeAnalysisSnapshot CurrentSnapshot { get; }
    RecipeAnalysisSnapshot? LastValidSnapshot { get; }

    Result<RecipeAnalysisSnapshot> AddStep(int index);
    Result<RecipeAnalysisSnapshot> RemoveStep(int index);
    Result<RecipeAnalysisSnapshot> ReplaceAction(int index, short actionId);
    Result<RecipeAnalysisSnapshot> UpdateProperty(int index, ColumnIdentifier column, object value);
    Result<RecipeAnalysisSnapshot> LoadRecipe(Recipe recipe);
}