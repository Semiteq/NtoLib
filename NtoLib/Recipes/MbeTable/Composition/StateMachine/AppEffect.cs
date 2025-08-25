#nullable enable
using System;

namespace NtoLib.Recipes.MbeTable.Composition.StateMachine
{
    /// <summary>
    /// Declarative effects triggered by AppStateMachine after Reduce.
    /// Executed by RecipeEffectsHandler (effect runner).
    /// </summary>
    public abstract record AppEffect(Guid OpId);

    public sealed record LoadRecipeEffect(Guid OpId, string FilePath) : AppEffect(OpId);
    public sealed record SaveRecipeEffect(Guid OpId, string FilePath) : AppEffect(OpId);
    public sealed record SendRecipeEffect(Guid OpId) : AppEffect(OpId);
    public sealed record ReadRecipeEffect(Guid OpId) : AppEffect(OpId);
}