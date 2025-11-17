using System;

using InSAT.OPC;

namespace NtoLib.Recipes.MbeTable.ModuleInfrastructure.PinDataManager;

/// <summary>
/// Polls raw pins, maintains last good snapshot, raises granular events on field changes.
/// Freezes while any required pin has bad quality.
/// </summary>
internal sealed class RecipeRuntimeStatePoller : IRecipeRuntimeState
{
    private readonly IPinAccessor _pins;
    private readonly float _epsilon;

    private bool _initialized;
    private bool _qualityOk;

    public event Action<bool>? RecipeActiveChanged;
    public event Action<bool>? SendEnabledChanged;
    public event Action<StepPhase>? StepPhaseChanged;

    public RecipeRuntimeSnapshot Current { get; private set; }

    // Pin IDs injected via MbeTableFB (made internal there, or provide accessors)
    private readonly int _idRecipeActive;
    private readonly int _idSendEnabled;
    private readonly int _idCurrentLine;
    private readonly int _idFor1;
    private readonly int _idFor2;
    private readonly int _idFor3;
    private readonly int _idStepElapsed;

    public RecipeRuntimeStatePoller(
        IPinAccessor pins,
        float epsilon,
        int idRecipeActive,
        int idSendEnabled,
        int idCurrentLine,
        int idForLoop1,
        int idForLoop2,
        int idForLoop3,
        int idStepCurrentTime)
    {
        _pins = pins ?? throw new ArgumentNullException(nameof(pins));
        _epsilon = epsilon;

        _idRecipeActive = idRecipeActive;
        _idSendEnabled = idSendEnabled;
        _idCurrentLine = idCurrentLine;
        _idFor1 = idForLoop1;
        _idFor2 = idForLoop2;
        _idFor3 = idForLoop3;
        _idStepElapsed = idStepCurrentTime;
    }

    public void Poll()
    {
        // 1. Check quality first (all required pins must be GOOD)
        bool qualityGood =
            AllGood(_idRecipeActive, _idSendEnabled, _idCurrentLine, _idFor1, _idFor2, _idFor3, _idStepElapsed);

        if (!qualityGood)
        {
            _qualityOk = false;
            return; // freeze, keep last snapshot
        }

        // Transition from bad → good: reinitialize events
        bool recovered = !_qualityOk;
        _qualityOk = true;

        var newSnap = ReadSnapshot();

        if (!_initialized || recovered)
        {
            Current = newSnap;
            _initialized = true;
            FireAllInitial(newSnap);
            return;
        }

        var prev = Current;

        // Decide changes
        bool recipeActiveChanged = prev.RecipeActive != newSnap.RecipeActive;
        bool sendEnabledChanged = prev.SendEnabled != newSnap.SendEnabled;

        bool phaseChanged =
            prev.StepIndex != newSnap.StepIndex
            || prev.ForLevel1Count != newSnap.ForLevel1Count
            || prev.ForLevel2Count != newSnap.ForLevel2Count
            || prev.ForLevel3Count != newSnap.ForLevel3Count;
        
        Current = newSnap;

        if (recipeActiveChanged)
            SafeInvoke(() => RecipeActiveChanged?.Invoke(newSnap.RecipeActive));

        if (sendEnabledChanged)
            SafeInvoke(() => SendEnabledChanged?.Invoke(newSnap.SendEnabled));

        if (phaseChanged)
        {
            var phase = new StepPhase(
                newSnap.StepIndex);
            SafeInvoke(() => StepPhaseChanged?.Invoke(phase));
        }
    }

    private RecipeRuntimeSnapshot ReadSnapshot()
    {
        return new RecipeRuntimeSnapshot(
            RecipeActive: ReadBool(_idRecipeActive),
            SendEnabled: ReadBool(_idSendEnabled),
            StepIndex: ReadInt(_idCurrentLine),
            ForLevel1Count: ReadInt(_idFor1),
            ForLevel2Count: ReadInt(_idFor2),
            ForLevel3Count: ReadInt(_idFor3),
            StepElapsedSeconds: ReadFloat(_idStepElapsed)
        );
    }

    private bool AllGood(params int[] ids)
    {
        foreach (var id in ids)
        {
            if (_pins.GetQuality(id) != OpcQuality.Good)
                return false;
        }
        return true;
    }

    private bool ReadBool(int id) => _pins.GetValue<bool>(id);
    private int ReadInt(int id) => _pins.GetValue<int>(id);
    private float ReadFloat(int id) => _pins.GetValue<float>(id);

    private void FireAllInitial(RecipeRuntimeSnapshot s)
    {
        SafeInvoke(() => RecipeActiveChanged?.Invoke(s.RecipeActive));
        SafeInvoke(() => SendEnabledChanged?.Invoke(s.SendEnabled));
        var phase = new StepPhase(s.StepIndex);
        SafeInvoke(() => StepPhaseChanged?.Invoke(phase));
    }

    private static void SafeInvoke(Action a)
    {
        try { a(); } catch { /* swallow or log via injected logger if needed */ }
    }
}