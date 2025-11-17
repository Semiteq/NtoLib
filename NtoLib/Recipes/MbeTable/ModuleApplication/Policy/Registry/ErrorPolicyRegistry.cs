using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;
using NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Warnings;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Warnings;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Policy.Registry;

public sealed class ErrorPolicyRegistry
{
    private readonly Dictionary<Type, ErrorPolicy> _policies = new();

    public ErrorPolicyRegistry()
    {
        RegisterModbusTcpPolicies();
        RegisterApplicationPolicies();
        RegisterCorePolicies();
    }


    private void RegisterModbusTcpPolicies()
    {
        Register<ModbusTcpZeroRowsWarning>(ErrorSeverity.Warning, BlockingScope.SaveAndSend);
    }

    private void RegisterApplicationPolicies()
    {
        Register<ApplicationRecipeActiveWarning>(ErrorSeverity.Warning, BlockingScope.NotSave);
    }

    private void RegisterCorePolicies()
    {
        Register<CoreForLoopUnmatchedWarning>(ErrorSeverity.Warning, BlockingScope.SaveAndSend);
        Register<CoreForLoopInvalidIterationCountWarning>(ErrorSeverity.Warning, BlockingScope.SaveAndSend);
        Register<CoreForLoopMaxDepthExceededWarning>(ErrorSeverity.Warning, BlockingScope.SaveAndSend);
        Register<CoreEmptyRecipeWarning>(ErrorSeverity.Info, BlockingScope.SaveAndSend);
    }

    private void Register<T>(ErrorSeverity severity, BlockingScope scope) where T : IReason
    {
        _policies[typeof(T)] = new ErrorPolicy(severity, scope);
    }

    public ErrorPolicy GetPolicy(IReason reason)
    {
        if (reason == null) throw new ArgumentNullException(nameof(reason));

        var type = reason.GetType();
        return _policies.TryGetValue(type, out var policy)
            ? policy
            : new ErrorPolicy(ErrorSeverity.Info, BlockingScope.None);
    }

    public bool Blocks(IReason reason, BlockingScope scope)
    {
        var policy = GetPolicy(reason);
        return (policy.BlockingScope & scope) != 0;
    }

    public ErrorSeverity GetSeverity(IReason reason)
    {
        return GetPolicy(reason).Severity;
    }
}