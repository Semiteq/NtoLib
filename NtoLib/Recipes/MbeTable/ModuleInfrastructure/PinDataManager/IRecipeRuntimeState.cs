using System;

namespace NtoLib.Recipes.MbeTable.ModuleInfrastructure.PinDataManager;

/// <summary>
/// Provides current recipe execution state and events for significant changes.
/// </summary>
public interface IRecipeRuntimeState
{
    /// <summary>Raised when runtime signals that recipe active flag changed.</summary>
    event Action<bool> RecipeActiveChanged;

    /// <summary>Raised when runtime signals that send-enabled flag changed.</summary>
    event Action<bool> SendEnabledChanged;

    /// <summary>Raised when step index or any for-loop counter changes.</summary>
    event Action<StepPhase> StepPhaseChanged;

    /// <summary>The last valid snapshot (frozen while quality is degraded).</summary>
    RecipeRuntimeSnapshot Current { get; }

    /// <summary>
    /// Polls PLC pins and (if quality is good) updates internal snapshot and fires change events.
    /// Should be called once per scan tick from MbeTableFB.UpdateData().
    /// </summary>
    void Poll();
}