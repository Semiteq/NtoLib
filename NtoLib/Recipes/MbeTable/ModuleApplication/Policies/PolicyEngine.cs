using System;
using System.Collections.Generic;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.ModuleApplication.ErrorPolicy;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Policies;

public sealed class PolicyEngine : IPolicyEngine
{
    private readonly ErrorPolicyRegistry _registry;

    public PolicyEngine(ErrorPolicyRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public OperationDecision Decide(OperationId operation, IEnumerable<IReason> reasons)
    {
        if (reasons == null) return OperationDecision.Allowed();

        var scope = MapOperationToScope(operation);

        IReason? blockingError = null;
        IReason? blockingWarning = null;

        foreach (var r in reasons)
        {
            if (!_registry.Blocks(r, scope)) continue;

            var severity = _registry.GetSeverity(r);
            if (severity == ErrorSeverity.Error || severity == ErrorSeverity.Critical)
            {
                blockingError ??= r;
            }
            else if (severity == ErrorSeverity.Warning)
            {
                blockingWarning ??= r;
            }

            if (blockingError != null) break;
        }

        if (blockingError != null) return OperationDecision.BlockedError(blockingError);
        if (blockingWarning != null) return OperationDecision.BlockedWarning(blockingWarning);
        return OperationDecision.Allowed();
    }

    public bool IsBlocking(OperationId operation, IReason reason)
    {
        if (reason == null) return false;
        var scope = MapOperationToScope(operation);
        return _registry.Blocks(reason, scope);
    }

    private static BlockingScope MapOperationToScope(OperationId operation) =>
        operation switch
        {
            OperationId.Save => BlockingScope.Save,
            OperationId.Send => BlockingScope.Send,
            OperationId.Load => BlockingScope.Load,
            OperationId.Receive => BlockingScope.Load,
            OperationId.AddStep => BlockingScope.Edit,
            OperationId.RemoveStep => BlockingScope.Edit,
            OperationId.EditCell => BlockingScope.Edit,
            _ => BlockingScope.None
        };
}