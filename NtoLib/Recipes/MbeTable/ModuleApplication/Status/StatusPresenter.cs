using System;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Recipes.MbeTable.ServiceStatus;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Status;

public sealed class StatusPresenter : IStatusPresenter
{
    private readonly IStatusService _status;

    public StatusPresenter(IStatusService status)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
    }

    public void Clear() => _status.Clear();

    public void ShowSuccess(string message)
    {
        _status.Clear();
        if (!string.IsNullOrWhiteSpace(message))
            _status.ShowInfo(ToSentence(message));
    }

    public void ShowWarning(string message) => _status.ShowWarning(ToSentence(message));

    public void ShowError(string message) => _status.ShowError(ToSentence(message));

    public void ShowInfo(string message) => _status.ShowInfo(ToSentence(message));

    public static string BuildWarningMessage(Result result, string operationRu)
    {
        var warningsRu = result.Reasons
            .OfType<BilingualWarning>()
            .Select(w => w.MessageRu)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var message = warningsRu.Count == 0
            ? "завершена с предупреждением"
            : string.Join("; ", warningsRu);

        return $"{ToSentence(operationRu)}: {message}";
    }

    public static string BuildErrorMessage(Result result, string operationRu)
    {
        var bilingualErrors = result.Reasons
            .OfType<IError>()
            .OfType<BilingualError>()
            .Select(e => e.MessageRu)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Reverse()
            .ToList();

        if (bilingualErrors.Count > 0)
            return $"Не удалось {operationRu}: {string.Join(" → ", bilingualErrors)}";

        var genericErrors = result.Reasons
            .OfType<IError>()
            .Select(e => string.IsNullOrWhiteSpace(e.Message) ? "Ошибка" : e.Message)
            .Reverse()
            .ToList();

        var final = genericErrors.Count > 0 ? string.Join(" → ", genericErrors) : "Неизвестная ошибка";
        return $"Не удалось {operationRu}: {final}";
    }

    private static string ToSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "Операция";
        return char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}