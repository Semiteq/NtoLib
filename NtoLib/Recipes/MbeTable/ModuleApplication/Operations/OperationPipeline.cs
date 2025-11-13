using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;
using NtoLib.Recipes.MbeTable.ModuleApplication.Policies;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleApplication.Status;
using NtoLib.Recipes.MbeTable.ModuleCore;
using NtoLib.Recipes.MbeTable.ModuleCore.Attributes;
using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations;

public sealed class OperationPipeline : IOperationPipeline
{
    private readonly ILogger<OperationPipeline> _logger;
    private readonly IStateProvider _state;
    private readonly IPolicyEngine _policy;
    private readonly IStatusPresenter _status;
    private readonly IPolicyReasonsSink _reasonsSink;

    public OperationPipeline(
        ILogger<OperationPipeline> logger,
        IStateProvider state,
        IPolicyEngine policy,
        IStatusPresenter status,
        IPolicyReasonsSink reasonsSink)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _status = status ?? throw new ArgumentNullException(nameof(status));
        _reasonsSink = reasonsSink ?? throw new ArgumentNullException(nameof(reasonsSink));
    }

    public async Task<Result> RunAsync(
        OperationId operationId,
        OperationKind operationKind,
        Func<Task<Result>> execute,
        string? successMessage = null,
        bool affectsRecipe = false)
    {
        if (execute == null) throw new ArgumentNullException(nameof(execute));

        var gateResult = AcquireGate(operationKind, operationId);
        if (gateResult.IsFailed)
        {
            LogOperationBlocked(operationId);
            ShowErrorStatus(gateResult.ToResult(), operationId);
            return gateResult.ToResult();
        }

        using (gateResult.Value)
        {
            try
            {
                _status.Clear();

                var operationResult = await execute().ConfigureAwait(false);
                var mergedReasons = operationResult.Reasons;

                if (ContainsErrors(mergedReasons))
                {
                    LogOperationFailedWithErrors(operationId);
                    ShowErrorStatus(operationResult, operationId);
                    return CreateFailureFromErrors(mergedReasons);
                }

                PresentOperationStatus(operationId, operationResult, mergedReasons, successMessage);

                if (affectsRecipe && operationResult.IsSuccess)
                {
                    UpdatePersistentReasons(mergedReasons);
                }

                return operationResult;
            }
            catch (Exception ex)
            {
                return HandleUnexpectedException(ex, operationId);
            }
        }
    }

    public Result Run(
        OperationId operationId,
        OperationKind operationKind,
        Func<Result> execute,
        string? successMessage = null,
        bool affectsRecipe = false)
    {
        if (execute == null) throw new ArgumentNullException(nameof(execute));

        Task<Result> ExecuteAsync()
        {
            return Task.FromResult(execute());
        }

        return RunAsync(operationId, operationKind, ExecuteAsync, successMessage, affectsRecipe)
            .GetAwaiter()
            .GetResult();
    }

    public async Task<Result<T>> RunAsync<T>(
        OperationId operationId,
        OperationKind operationKind,
        Func<Task<Result<T>>> execute,
        string? successMessage = null,
        bool affectsRecipe = false)
    {
        if (execute == null) throw new ArgumentNullException(nameof(execute));

        var gateResult = AcquireGate(operationKind, operationId);
        if (gateResult.IsFailed)
        {
            LogOperationBlocked(operationId);
            ShowErrorStatus(gateResult.ToResult(), operationId);
            return gateResult.ToResult<T>();
        }

        using (gateResult.Value)
        {
            try
            {
                _status.Clear();

                var operationResult = await execute().ConfigureAwait(false);
                var mergedReasons = ExtractMergedReasons(operationResult);

                if (ContainsErrors(mergedReasons))
                {
                    LogOperationFailedWithErrors(operationId);
                    ShowErrorStatus(operationResult.ToResult(), operationId);
                    return CreateFailureFromErrors<T>(mergedReasons);
                }

                PresentOperationStatus(operationId, operationResult.ToResult(), mergedReasons, successMessage);

                if (affectsRecipe && operationResult.IsSuccess)
                {
                    UpdatePersistentReasons(mergedReasons);
                }

                return operationResult;
            }
            catch (Exception ex)
            {
                return HandleUnexpectedException<T>(ex, operationId);
            }
        }
    }

    public Result<T> Run<T>(
        OperationId operationId,
        OperationKind operationKind,
        Func<Result<T>> execute,
        string? successMessage = null,
        bool affectsRecipe = false)
    {
        if (execute == null) throw new ArgumentNullException(nameof(execute));

        Task<Result<T>> ExecuteAsync()
        {
            return Task.FromResult(execute());
        }

        return RunAsync(operationId, operationKind, ExecuteAsync, successMessage, affectsRecipe)
            .GetAwaiter()
            .GetResult();
    }

    private Result<IDisposable> AcquireGate(OperationKind operationKind, OperationId operationId)
    {
        if (operationKind == OperationKind.None)
        {
            return Result.Ok<IDisposable>(new NullDisposable());
        }

        return _state.BeginOperation(operationKind, operationId);
    }

    private static IEnumerable<IReason> ExtractMergedReasons<T>(Result<T> operationResult)
    {
        if (operationResult.IsSuccess && operationResult.Value is ValidationSnapshot snapshot)
        {
            var snapshotReasons = snapshot.Reasons ?? Array.Empty<IReason>();
            return operationResult.Reasons.Concat(snapshotReasons);
        }

        return operationResult.Reasons;
    }

    private static bool ContainsErrors(IEnumerable<IReason> reasons)
    {
        return reasons.OfType<IError>().Any();
    }

    private static Result CreateFailureFromErrors(IEnumerable<IReason> reasons)
    {
        var errors = reasons.OfType<IError>().ToArray();
        return Result.Fail(errors);
    }

    private static Result<T> CreateFailureFromErrors<T>(IEnumerable<IReason> reasons)
    {
        var errors = reasons.OfType<IError>().ToArray();
        return Result.Fail<T>(errors);
    }

    private void PresentOperationStatus(
        OperationId operationId,
        Result baseResult,
        IEnumerable<IReason> mergedReasons,
        string? successMessage)
    {
        var decision = _policy.Decide(operationId, mergedReasons);

        if (decision.Kind == DecisionKind.BlockedError)
        {
            ShowErrorStatus(baseResult, operationId);
            return;
        }

        if (decision.Kind == DecisionKind.BlockedWarning)
        {
            ShowWarningStatus(baseResult, operationId);
            return;
        }

        var hasWarnings = mergedReasons.OfType<BilingualWarning>().Any();
        if (hasWarnings)
        {
            ShowWarningStatus(baseResult, operationId);
            return;
        }

        ShowSuccessStatusIfNeeded(operationId, successMessage);
    }

    private void ShowSuccessStatusIfNeeded(OperationId operationId, string? successMessage)
    {
        var messageKind = DetermineCompletionMessageKind(operationId);

        if (messageKind == CompletionMessageKind.Success && !string.IsNullOrWhiteSpace(successMessage))
        {
            _status.ShowSuccess(successMessage);
            return;
        }

        if (messageKind == CompletionMessageKind.Info && !string.IsNullOrWhiteSpace(successMessage))
        {
            _status.ShowInfo(successMessage);
            return;
        }

        _status.Clear();
    }

    private void UpdatePersistentReasons(IEnumerable<IReason> mergedReasons)
    {
        var persistentReasons = mergedReasons ?? Enumerable.Empty<IReason>();
        _reasonsSink.SetPolicyReasons(persistentReasons);
    }

    private void LogOperationBlocked(OperationId operationId)
    {
        _logger.LogInformation("Operation [{Operation}] blocked before start", operationId);
    }

    private void LogOperationFailedWithErrors(OperationId operationId)
    {
        _logger.LogWarning("Operation [{Operation}] finished with error reasons, forcing failure", operationId);
    }

    private void ShowErrorStatus(Result result, OperationId operationId)
    {
        var operationNameRu = GetOperationNameRu(operationId);
        var errorMessage = StatusPresenter.BuildErrorMessage(result, operationNameRu);
        _status.ShowError(errorMessage);
    }

    private void ShowWarningStatus(Result result, OperationId operationId)
    {
        var operationNameRu = GetOperationNameRu(operationId);
        var warningMessage = StatusPresenter.BuildWarningMessage(result, operationNameRu);
        _status.ShowWarning(warningMessage);
    }

    private Result HandleUnexpectedException(Exception ex, OperationId operationId)
    {
        _logger.LogError(ex, "Operation [{Operation}] failed unexpectedly", operationId);
        var error = Result.Fail(new Error("Unexpected operation error").CausedBy(ex));
        ShowErrorStatus(error, operationId);
        return error;
    }

    private Result<T> HandleUnexpectedException<T>(Exception ex, OperationId operationId)
    {
        _logger.LogError(ex, "Operation [{Operation}] failed unexpectedly", operationId);
        var error = Result.Fail(new Error("Unexpected operation error").CausedBy(ex));
        ShowErrorStatus(error, operationId);
        return error.ToResult<T>();
    }

    private static string GetOperationNameRu(OperationId id)
    {
        return id switch
        {
            OperationId.Load => "загрузка рецепта",
            OperationId.Save => "сохранение рецепта",
            OperationId.Send => "отправка рецепта",
            OperationId.Receive => "чтение рецепта",
            OperationId.AddStep => "добавление строки",
            OperationId.RemoveStep => "удаление строки",
            OperationId.EditCell => "обновление ячейки",
            _ => "операция"
        };
    }

    private static CompletionMessageKind DetermineCompletionMessageKind(OperationId id)
    {
        return id switch
        {
            OperationId.Save => CompletionMessageKind.Success,
            OperationId.Send => CompletionMessageKind.Success,
            OperationId.Load => CompletionMessageKind.Info,
            OperationId.Receive => CompletionMessageKind.Info,
            OperationId.AddStep => CompletionMessageKind.Info,
            OperationId.RemoveStep => CompletionMessageKind.Info,
            OperationId.EditCell => CompletionMessageKind.None,
            _ => CompletionMessageKind.Info
        };
    }

    private enum CompletionMessageKind
    {
        None,
        Info,
        Success
    }

    private sealed class NullDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}