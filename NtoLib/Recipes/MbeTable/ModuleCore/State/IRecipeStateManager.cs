using NtoLib.Recipes.MbeTable.ModuleCore.Snapshot;

namespace NtoLib.Recipes.MbeTable.ModuleCore.State;

public interface IRecipeStateManager
{
	RecipeAnalysisSnapshot Current { get; }
	RecipeAnalysisSnapshot? LastValid { get; }
	void Update(RecipeAnalysisSnapshot snapshot);
}
