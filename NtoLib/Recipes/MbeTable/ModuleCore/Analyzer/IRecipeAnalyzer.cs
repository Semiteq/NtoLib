using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Snapshot;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Analyzer;

/// <summary>
/// Orchestrates full recipe analysis pipeline.
/// </summary>
public interface IRecipeAnalyzer
{
	RecipeAnalysisSnapshot Analyze(Recipe recipe);
}
