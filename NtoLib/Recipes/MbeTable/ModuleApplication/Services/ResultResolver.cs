using System;
using FluentResults;
using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;
using NtoLib.Recipes.MbeTable.ServiceStatus;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Services;

public sealed class ResultResolver
{
    private readonly IStatusService _status;
    private readonly ErrorDefinitionRegistry _registry;

    public ResultResolver(IStatusService statusService, ErrorDefinitionRegistry registry)
    {
        _status = statusService ?? throw new ArgumentNullException(nameof(statusService));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public void Resolve(Result result, string operation, string successMessage = null)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("Operation cannot be empty", nameof(operation));

        var status = result.GetStatus();
        switch (status)
        {
            case ResultStatus.Failure:
                HandleFailure(result, operation);
                break;
            case ResultStatus.Warning:
                HandleWarning(result, operation);
                break;
            case ResultStatus.Success:
                HandleSuccess(successMessage);
                break;
            default:
                throw new InvalidOperationException("Unknown result status");
        }
    }

    public void Resolve<T>(Result<T> result, string operation, string successMessage = null)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        Resolve(result.ToResult(), operation, successMessage);
    }

    private void HandleFailure(Result result, string operation)
    {
        var code = result.TryGetCode(out var c) ? c : Codes.UnknownError;
        var message = $"Не удалось {operation}: [{(int)code}] {_registry.GetMessage(code)}";
        _status.ShowError(message);
    }

    private void HandleWarning(Result result, string operation)
    {
        if (!result.TryGetCode(out var code)) code = Codes.UnknownError;
        var message = $"{ToSentence(operation)} завершена с предупреждением: [{(int)code}] {_registry.GetMessage(code)}";
        _status.ShowWarning(message);
    }

    private void HandleSuccess(string successMessage)
    {
        _status.Clear();

        if (!string.IsNullOrWhiteSpace(successMessage))
        {
            _status.ShowInfo(ToSentence(successMessage));
        }
    }


    private static string ToSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "Операция";
        return char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}