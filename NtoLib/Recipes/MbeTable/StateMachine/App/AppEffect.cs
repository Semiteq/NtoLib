#nullable enable
using System;

namespace NtoLib.Recipes.MbeTable.StateMachine.App
{
    /// <summary>
    /// Declarative effects triggered by AppStateMachine after Reduce.
    /// Executed by RecipeEffectsHandler (effect runner).
    /// </summary>
    public abstract record AppEffect(Guid OpId);

    public sealed record ReadRecipeEffect(Guid OpId, string FilePath) : AppEffect(OpId);
    public sealed record SaveRecipeEffect(Guid OpId, string FilePath) : AppEffect(OpId);
    public sealed record SendRecipeEffect(Guid OpId) : AppEffect(OpId);
    public sealed record ReceiveRecipeEffect(Guid OpId) : AppEffect(OpId);
}