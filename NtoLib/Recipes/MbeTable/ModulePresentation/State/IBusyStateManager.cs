using System;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.State;

/// <summary>
/// Centralized, thread-safe manager that locks UI during long-running operations
/// and exposes IDisposable scope for easy <c>using</c> statement.
/// </summary>
public interface IBusyStateManager
{
    /// <summary>
    /// True when UI is busy (any scoped operation active).
    /// </summary>
    bool IsBusy { get; }

    /// <summary>
    /// Enters busy state and returns disposable handle that will
    /// automatically exit busy state when disposed.
    /// </summary>
    IDisposable Enter(OperationKind operation);

    /// <summary>
    /// Raised when <see cref="IsBusy"/> flag toggles.
    /// </summary>
    event Action<bool>? BusyStateChanged;
}