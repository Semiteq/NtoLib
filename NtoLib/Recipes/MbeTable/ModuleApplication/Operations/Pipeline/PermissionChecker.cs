using NtoLib.Recipes.MbeTable.ModuleApplication.State;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;

internal sealed class PermissionChecker
{
    private readonly IStateProvider _state;

    public PermissionChecker(IStateProvider state)
    {
        _state = state;
    }

    public OperationDecision Check(IOperationDefinition op)
    {
        return _state.Evaluate(op.Id);
    }
}