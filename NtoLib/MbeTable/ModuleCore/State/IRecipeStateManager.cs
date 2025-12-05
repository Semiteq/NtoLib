using NtoLib.MbeTable.ModuleCore.Snapshot;

namespace NtoLib.MbeTable.ModuleCore.State;

public interface IRecipeStateManager
{
	RecipeAnalysisSnapshot Current { get; }
	RecipeAnalysisSnapshot? LastValid { get; }
	void Update(RecipeAnalysisSnapshot snapshot);
}
