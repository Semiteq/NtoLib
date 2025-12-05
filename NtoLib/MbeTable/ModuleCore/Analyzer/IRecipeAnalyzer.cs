using NtoLib.MbeTable.ModuleCore.Entities;
using NtoLib.MbeTable.ModuleCore.Snapshot;

namespace NtoLib.MbeTable.ModuleCore.Analyzer;

/// <summary>
/// Orchestrates full recipe analysis pipeline.
/// </summary>
public interface IRecipeAnalyzer
{
	RecipeAnalysisSnapshot Analyze(Recipe recipe);
}
