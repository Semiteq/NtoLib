using System;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Recipes.MbeTable.ServiceStatus;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Services;

public sealed class ResultResolver
{
    private readonly IStatusService _status;

    public ResultResolver(IStatusService statusService)
    {
        _status = statusService ?? throw new ArgumentNullException(nameof(statusService));
    }

    public void Resolve(Result result, string operation, string successMessage = null)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("Operation cannot be empty", nameof(operation));

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
        var message = ExtractRussianMessage(result);
        _status.ShowError($"Не удалось {operation}: {message}");
    }

    private void HandleWarning(Result result, string operation)
    {
        var message = ExtractRussianMessage(result);
        _status.ShowWarning($"{ToSentence(operation)} завершена с предупреждением: {message}");
    }

    private void HandleSuccess(string successMessage)
    {
        _status.Clear();

        if (!string.IsNullOrWhiteSpace(successMessage))
        {
            _status.ShowInfo(ToSentence(successMessage));
        }
    }

    private static string ExtractRussianMessage(Result result)
    {
        var bilingualErrors = result.Errors
            .OfType<BilingualError>()
            .Select(e => e.MessageRu)
            .ToList();

        if (bilingualErrors.Any())
            return string.Join("; ", bilingualErrors);

        var fallbackMessages = result.Errors
            .Select(e => e.Message)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToList();

        return fallbackMessages.Any()
            ? string.Join("; ", fallbackMessages)
            : "Неизвестная ошибка";
    }

    private static string ToSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "Операция";
        return char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}