using System;
using System.Collections.Generic;
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

    public void Clear()
    {
        _status.Clear();
    }

    public void Resolve(Result result, string operation, string successMessage = null)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("Operation cannot be empty", nameof(operation));

        if (result.IsFailed)
        {
            HandleFailure(result, operation);
        }
        else if (result.Reasons.OfType<BilingualWarning>().Any())
        {
            HandleWarning(result, operation);
        }
        else
        {
            HandleSuccess(successMessage);
        }
    }

    public void Resolve<T>(Result<T> result, string operation, string successMessage = null)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        Resolve(result.ToResult(), operation, successMessage);
    }

    private void HandleFailure(Result result, string operation)
    {
        var allErrors = CollectAllErrors(result).ToList();
        var message = ExtractRussianMessage(allErrors);
        _status.ShowError($"Не удалось {operation}: {message}");
    }

    private void HandleWarning(Result result, string operation)
    {
        var warnings = result.Reasons.OfType<BilingualWarning>().ToList();
        var message = string.Join("; ", warnings.Select(w => w.MessageRu));
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

    private static IEnumerable<IError> CollectAllErrors(Result result)
    {
        var visited = new HashSet<IError>(ReferenceEqualityComparer.Instance);
        var queue = new Queue<IError>(result.Errors);

        while (queue.Count > 0)
        {
            var error = queue.Dequeue();
            if (!visited.Add(error))
                continue;

            yield return error;

            foreach (var reason in error.Reasons.OfType<IError>())
            {
                queue.Enqueue(reason);
            }
        }
    }

    private static string ExtractRussianMessage(IEnumerable<IError> errors)
    {
        var bilingualMessages = errors
            .OfType<BilingualError>()
            .Select(e => e.MessageRu)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Reverse()
            .ToList();

        if (bilingualMessages.Any())
            return string.Join(" → ", bilingualMessages);

        var fallbackMessages = errors
            .Select(e => e.Message)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Reverse()
            .ToList();

        return fallbackMessages.Any()
            ? string.Join(" → ", fallbackMessages)
            : "Неизвестная ошибка";
    }

    private static string ToSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "Операция";
        return char.ToUpperInvariant(text[0]) + text.Substring(1);
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<IError>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public bool Equals(IError x, IError y) => ReferenceEquals(x, y);
        public int GetHashCode(IError obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}