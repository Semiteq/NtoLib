using NtoLib.Recipes.MbeTable.ModuleInfrastructure.PinDataManager;

namespace Tests.MbeTable.Core.Helpers;

/// <summary>
/// Helper for building RecipeRuntimeSnapshot instances in tests.
/// </summary>
public static class RuntimeSnapshotBuilder
{
	public static RecipeRuntimeSnapshot CreateActive(
		int stepIndex,
		float stepElapsed,
		int for1 = 0,
		int for2 = 0,
		int for3 = 0)
	{
		return new RecipeRuntimeSnapshot(
			RecipeActive: true,
			SendEnabled: true,
			StepIndex: stepIndex,
			ForLevel1Count: for1,
			ForLevel2Count: for2,
			ForLevel3Count: for3,
			StepElapsedSeconds: stepElapsed
		);
	}

	public static RecipeRuntimeSnapshot CreateInactive()
	{
		return new RecipeRuntimeSnapshot(
			RecipeActive: false,
			SendEnabled: true,
			StepIndex: 0,
			ForLevel1Count: 0,
			ForLevel2Count: 0,
			ForLevel3Count: 0,
			StepElapsedSeconds: 0f
		);
	}
}
