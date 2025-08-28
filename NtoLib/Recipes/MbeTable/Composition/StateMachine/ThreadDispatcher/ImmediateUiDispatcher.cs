using System;
using NtoLib.Recipes.MbeTable.Composition.StateMachine.Contracts;

namespace NtoLib.Recipes.MbeTable.Composition.StateMachine.ThreadDispatcher;

/// <summary>
/// Fallback dispatcher that runs the action inline on the current thread.
/// Use only as a default placeholder until a real UI dispatcher is attached.
/// </summary>
public sealed class ImmediateUiDispatcher : IUiDispatcher
{
    public void Post(Action action)
    {
        if (action == null) return;
        action();
    }
}