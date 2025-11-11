using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.Errors;
using NtoLib.Recipes.MbeTable.ModuleApplication.Warnings;
using NtoLib.Recipes.MbeTable.ServiceCsv.Errors;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Errors;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.ErrorPolicy;

public sealed class ErrorPolicyRegistry
{
    private readonly Dictionary<Type, ErrorPolicy> _policies = new();

    public ErrorPolicyRegistry()
    {
        RegisterCsvPolicies();
        RegisterModbusTcpPolicies();
        RegisterCorePolicies();
    }

    private void RegisterCsvPolicies()
    {
        Register<CsvHeaderMismatchError>(ErrorSeverity.Critical, BlockingScope.AllOperations);
        Register<CsvInvalidDataError>(ErrorSeverity.Error, BlockingScope.None);
        Register<CsvEmptyHeaderError>(ErrorSeverity.Critical, BlockingScope.AllOperations);
    }

    private void RegisterModbusTcpPolicies()
    {
        Register<ModbusTcpConnectionFailedError>(ErrorSeverity.Critical, BlockingScope.AllOperations);
        Register<ModbusTcpCapacityExceededError>(ErrorSeverity.Error, BlockingScope.SaveAndSend);
        Register<ModbusTcpVerificationFailedError>(ErrorSeverity.Error, BlockingScope.None);
        Register<ModbusTcpInvalidResponseError>(ErrorSeverity.Error, BlockingScope.None);
        Register<ModbusTcpReadFailedError>(ErrorSeverity.Error, BlockingScope.None);
        Register<ModbusTcpFailedError>(ErrorSeverity.Error, BlockingScope.None);
        Register<ModbusTcpTimeoutError>(ErrorSeverity.Error, BlockingScope.None);
        Register<ModbusTcpSerializationError>(ErrorSeverity.Error, BlockingScope.SaveAndSend);
    }

    private void RegisterCorePolicies()
    {
        Register<ApplicationEmptyRecipeWarning>(ErrorSeverity.Warning, BlockingScope.SaveAndSend);
        Register<ApplicationValidationFailedError>(ErrorSeverity.Error, BlockingScope.SaveAndSend);
        Register<ApplicationInvalidOperationError>(ErrorSeverity.Error, BlockingScope.None);
        Register<ApplicationRecipeActiveError>(ErrorSeverity.Error, BlockingScope.AllOperations);
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