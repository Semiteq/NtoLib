namespace NtoLib.Recipes.MbeTable.ModuleInfrastructure.PinDataManager;

/// <summary>
/// Immutable snapshot of recipe execution state derived from PLC pins.
/// Updated only when all required pins are in good quality.
/// </summary>
public readonly record struct RecipeRuntimeSnapshot(
    bool RecipeActive,
    bool SendEnabled,
    int StepIndex,
    int ForLevel1Count,
    int ForLevel2Count,
    int ForLevel3Count,
    float StepElapsedSeconds
);

/// <summary>
/// Logical execution phase (step + nested loop counters).
/// Any change in any component means a new phase.
/// </summary>
public readonly record struct StepPhase(
    int StepIndex
);