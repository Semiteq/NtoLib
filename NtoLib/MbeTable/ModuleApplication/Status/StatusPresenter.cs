using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.MbeTable.ResultsExtension;
using NtoLib.MbeTable.ServiceStatus;

namespace NtoLib.MbeTable.ModuleApplication.Status;

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
		if (string.IsNullOrWhiteSpace(message))
		{
			_status.Clear();
			return;
		}

		_status.ShowSuccess(ToSentence(message));
	}

	public void ShowWarning(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			_status.Clear();
			return;
		}

		_status.ShowWarning(ToSentence(message));
	}

	public void ShowError(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			_status.Clear();
			return;
		}

		_status.ShowError(ToSentence(message));
	}

	public void ShowInfo(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			_status.Clear();
			return;
		}

		_status.ShowInfo(ToSentence(message));
	}

	public static string BuildWarningMessage(Result result, string operationRu,
		IEnumerable<IReason>? reasonsOverride = null)
	{
		var reasons = reasonsOverride ?? result.Reasons;

		var warningsRu = reasons
			.OfType<BilingualWarning>()
			.Select(w => w.MessageRu)
			.Where(s => !string.IsNullOrWhiteSpace(s))
			.Distinct()
			.ToList();

		var message = warningsRu.Count == 0
			? "завершена с предупреждением"
			: string.Join("; ", warningsRu);

		return $"{ToSentence(operationRu)}: {message}";
	}

	public static string BuildErrorMessage(Result result, string operationRu,
		IEnumerable<IReason>? reasonsOverride = null)
	{
		var reasons = reasonsOverride ?? result.Reasons;

		var bilingualErrors = reasons
			.OfType<IError>()
			.OfType<BilingualError>()
			.Select(e => e.MessageRu)
			.Where(s => !string.IsNullOrWhiteSpace(s))
			.Reverse()
			.Distinct()
			.ToList();

		if (bilingualErrors.Count > 0)
			return $"Не удалось {operationRu}: {string.Join(" → ", bilingualErrors)}";

		var genericErrors = reasons
			.OfType<IError>()
			.Select(e => string.IsNullOrWhiteSpace(e.Message) ? "Ошибка" : e.Message)
			.Reverse()
			.Distinct()
			.ToList();

		var final = genericErrors.Count > 0 ? string.Join(" → ", genericErrors) : "Неизвестная ошибка";
		return $"Не удалось {operationRu}: {final}";
	}

	private static string ToSentence(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return "Операция";
		return char.ToUpperInvariant(text[0]) + text.Substring(1);
	}
}
