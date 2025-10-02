#nullable enable

using System;
using NtoLib.Recipes.MbeTable.StateMachine.Contracts;

namespace NtoLib.Recipes.MbeTable.StateMachine.ThreadDispatcher;

/// <summary>
/// Fallback dispatcher that runs the action inline on the current thread.
/// Use only as a default placeholder until a real UI dispatcher is attached.
/// </summary>
public sealed class ImmediateUiDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public void Post(Action action)
    {
        action?.Invoke();
    }
}