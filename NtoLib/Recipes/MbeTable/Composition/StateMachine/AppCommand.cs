using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Composition.StateMachine
{
    public abstract record AppCommand;

    // Lifecycle
    public sealed record EnterEditor : AppCommand;
    public sealed record EnterRuntime : AppCommand;

    // From internal services
    public sealed record VmLoopValidChanged(bool VmOk) : AppCommand;
    public sealed record PlcAvailabilityChanged(PlcRecipeAvailable Avail) : AppCommand;
    
    // UI intents (Requested)
    public sealed record LoadRecipeRequested(string FilePath) : AppCommand;
    public sealed record SaveRecipeRequested(string FilePath) : AppCommand;
    public sealed record SendRecipeRequested : AppCommand;

    // Completion (from effects)
    public sealed record LoadRecipeCompleted(Guid OpId, bool Success, string Message, IReadOnlyList<string>? Errors = null) : AppCommand;
    public sealed record SaveRecipeCompleted(Guid OpId, bool Success, string Message, IReadOnlyList<string>? Errors = null) : AppCommand;
    public sealed record SendRecipeCompleted(Guid OpId, bool Success, string Message, IReadOnlyList<string>? Errors = null) : AppCommand;

    // Messages
    public sealed record PostMessage(UiMessage Msg) : AppCommand;
    public sealed record AckMessage : AppCommand;
    public sealed record ClearMessage : AppCommand;
}