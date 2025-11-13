using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.State;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;

internal sealed class PostSuccessEffects
{
    private readonly IStateProvider _state;

    public PostSuccessEffects(IStateProvider state)
    {
        _state = state;
    }

    public void Apply(IOperationDefinition op, IReadOnlyList<IReason> reasons)
    {
        if (op.UpdatesPolicyReasons)
            _state.SetPolicyReasons(reasons);

        ApplyConsistencyEffect(op.ConsistencyEffect);
    }

    private void ApplyConsistencyEffect(ConsistencyEffect effect)
    {
        switch (effect)
        {
            case ConsistencyEffect.MarkConsistent:
                _state.SetRecipeConsistent(true);
                break;
            case ConsistencyEffect.MarkInconsistent:
                _state.SetRecipeConsistent(false);
                break;
            case ConsistencyEffect.None:
            default:
                break;
        }
    }
}