using System;
using System.Linq;
using System.Text;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.ServiceStatus;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Services;

public sealed class ResultResolver
{
    private readonly IStatusService _status;
    private readonly ILogger<ResultResolver> _logger;
    private readonly IErrorCatalog _catalog;

    public ResultResolver(IStatusService statusService,
        ILogger<ResultResolver> logger,
        IErrorCatalog catalog)
    {
        _status = statusService ?? throw new ArgumentNullException(nameof(statusService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public void Resolve(Result result, ResolveOptions options)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (options == null) throw new ArgumentNullException(nameof(options));

        if (result.IsFailed)
        {
            HandleError(result, options);
            return;
        }

        if (HasReasons(result))
        {
            HandleWarning(result, options);
            return;
        }

        if (!options.SilentOnPureSuccess)
        {
            var msg = $"{ToSentence(options.SuccessMessage)}";
            _status.ShowInfo(msg);
            _logger.LogInformation("{Operation} succeeded", options.Operation);
        }
    }

    public void Resolve<T>(Result<T> result, ResolveOptions options)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        Resolve(result.ToResult(), options);
    }

    private void HandleError(Result result, ResolveOptions options)
    {
        var hasCode = result.TryGetCode(out var code);
        var uiText = hasCode
            ? $"[{(int)code}] {_catalog.GetMessageOrDefault(code)}"
            : $"Не удалось {options.Operation}";

        _status.ShowError(uiText);

        var logDetails = BuildDetailedFailureLog(result);
        if (hasCode)
        {
            _logger.LogError("Failed to {Operation}. Code={Code} ({CodeInt}). Details: {Details}",
                options.Operation, code, (int)code, logDetails);
        }
        else
        {
            _logger.LogError("Failed to {Operation}. Details: {Details}", options.Operation, logDetails);
        }
    }

    private void HandleWarning(Result result, ResolveOptions options)
    {
        var warningMessage = TryGetWarningMessageFromCatalog(result)
                             ?? TryGetWarningMessageFromSuccesses(result)
                             ?? "Завершена с предупреждением";

        var uiText = $"{ToSentence(options.Operation)} завершена с предупреждением: {warningMessage}";
        _status.ShowWarning(uiText);

        var details = BuildDetailedReasonsLog(result);
        _logger.LogWarning("{Operation} completed with warnings. Details: {Details}", options.Operation, details);
    }

    private string? TryGetWarningMessageFromCatalog(Result result)
    {
        try
        {
            var firstSuccess = result.Successes.FirstOrDefault();
            if (firstSuccess?.Metadata.TryGetValue(nameof(Codes), out var metadataCode) == true
                && metadataCode is Codes code
                && _catalog.TryGetMessage(code, out var catalogMessage))
            {
                return catalogMessage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to retrieve warning message from catalog metadata");
        }

        return null;
    }

    private static string? TryGetWarningMessageFromSuccesses(Result result)
    {
        return result.Successes.FirstOrDefault()?.Message;
    }


    private static bool HasReasons(Result result)
    {
        return result.IsSuccess && result.Reasons.Count > 0;
    }

    private static string ToSentence(string operation)
    {
        if (string.IsNullOrWhiteSpace(operation)) return "Операция";
        return char.ToUpperInvariant(operation[0]) + operation.Substring(1);
    }

    private static string BuildDetailedFailureLog(Result result)
    {
        var sb = new StringBuilder();

        if (result.Errors.Count > 0)
        {
            sb.Append("Errors=[");
            sb.Append(string.Join(" | ", result.Errors.Select(e => e.Message)));
            sb.Append(']');
        }

        if (result.Reasons.Count > 0)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append("Reasons=[");
            sb.Append(string.Join(" | ", result.Reasons.Select(r => r.Message)));
            sb.Append(']');
        }

        var allMeta = result.Reasons.SelectMany(r => r.Metadata).ToList();
        if (allMeta.Count > 0)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append("Metadata=[");
            sb.Append(string.Join(", ", allMeta.Select(kv => $"{kv.Key}={kv.Value}")));
            sb.Append(']');
        }

        return sb.Length > 0 ? sb.ToString() : "No details available";
    }

    private static string BuildDetailedReasonsLog(Result result)
    {
        var sb = new StringBuilder();

        if (result.Reasons.Count > 0)
        {
            sb.Append("Reasons=[");
            sb.Append(string.Join(" | ", result.Reasons.Select(r => r.Message)));
            sb.Append(']');
        }

        var allMeta = result.Reasons.SelectMany(r => r.Metadata).ToList();
        if (allMeta.Count > 0)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append("Metadata=[");
            sb.Append(string.Join(", ", allMeta.Select(kv => $"{kv.Key}={kv.Value}")));
            sb.Append(']');
        }

        return sb.Length > 0 ? sb.ToString() : "No details available";
    }
}