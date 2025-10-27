using System;
using System.Linq;
using System.Text;

using FluentResults;

using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;
using NtoLib.Recipes.MbeTable.ServiceStatus;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Services;

public sealed class ResultResolver
{
    private readonly IStatusService _status;
    private readonly ErrorDefinitionRegistry _registry;

    public ResultResolver(
        IStatusService statusService,
        ErrorDefinitionRegistry registry)
    {
        _status = statusService ?? throw new ArgumentNullException(nameof(statusService));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public void Resolve(Result result, string operation, string successMessage = null)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("Operation cannot be empty", nameof(operation));

        switch (result.GetStatus())
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
        if (!result.TryGetCode(out var code))
        {
            _status.ShowError($"Не удалось {operation}");
            return;
        }

        var definition = _registry.GetDefinition(code);
        _status.ShowError($"[{(int)code}] {definition.Message}");
    }

    private void HandleWarning(Result result, string operation)
    {
        if (!result.TryGetCode(out var code))
        {
            _status.ShowWarning($"{ToSentence(operation)} завершена с предупреждением");
            return;
        }

        var definition = _registry.GetDefinition(code);
        _status.ShowWarning($"{ToSentence(operation)} завершена с предупреждением: [{(int)code}] {definition.Message}");
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

    private static string BuildDetails(Result result)
    {
        var sb = new StringBuilder();

        if (result.Errors.Count > 0)
        {
            sb.Append("Errors=[");
            sb.Append(string.Join(" | ", result.Errors.Select(FormatReason)));
            sb.Append(']');
        }

        var warnings = result.Reasons.Where(r => r is ValidationIssue).ToList();
        if (warnings.Count > 0)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append("Warnings=[");
            sb.Append(string.Join(" | ", warnings.Select(FormatReason)));
            sb.Append(']');
        }

        return sb.Length > 0 ? sb.ToString() : "No details available";
    }

    private static string FormatReason(IReason reason)
    {
        if (!reason.TryGetCode(out var code))
            return "Unknown";

        var metadata = reason.Metadata
            .Where(kv => kv.Key != "Code" && kv.Key != "Codes")
            .Select(kv => $"{kv.Key}={kv.Value}");

        var meta = string.Join(", ", metadata);
        return meta.Length > 0 
            ? $"Code={code}({(int)code}) {meta}" 
            : $"Code={code}({(int)code})";
    }
}