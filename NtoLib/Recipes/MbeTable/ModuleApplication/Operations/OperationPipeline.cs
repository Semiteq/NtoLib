using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;
using NtoLib.Recipes.MbeTable.ModuleApplication.Policies;
using NtoLib.Recipes.MbeTable.ModuleApplication.Recipes;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleApplication.Status;
using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations;

public sealed class OperationPipeline : IOperationPipeline
{
    private readonly ILogger<OperationPipeline> _logger;
    private readonly IStateProvider _state;
    private readonly IPolicyEngine _policy;
    private readonly IStatusPresenter _status;
    private readonly IValidationSnapshotProvider _snapshotProvider;
    private readonly IPolicyReasonsSink _reasonsSink;

    public OperationPipeline(
        ILogger<OperationPipeline> logger,
        IStateProvider state,
        IPolicyEngine policy,
        IStatusPresenter status,
        IValidationSnapshotProvider snapshotProvider,
        IPolicyReasonsSink reasonsSink)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _status = status ?? throw new ArgumentNullException(nameof(status));
        _snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
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

        Result<IDisposable>? gate = null;
        if (operationKind != OperationKind.None)
        {
            gate = _state.BeginOperation(operationKind, operationId);
            if (gate.IsFailed)
            {
                _logger.LogInformation("Operation [{Operation}] blocked before start", operationId);
                _status.ShowError(StatusPresenter.BuildErrorMessage(gate.ToResult(), GetOperationNameRu(operationId)));
                return gate.ToResult();
            }
        }

        using (gate?.Value)
        {
            try
            {
                _status.Clear();

                var result = await execute().ConfigureAwait(false);

                // Treat any IError as failure, even if IsSuccess is true.
                var errorReasons = result.Reasons.OfType<IError>().ToList();
                if (errorReasons.Count > 0)
                {
                    var failed = Result.Fail(errorReasons.ToArray());
                    _logger.LogWarning("Operation [{Operation}] finished with error reasons, forcing failure", operationId);
                    _status.ShowError(StatusPresenter.BuildErrorMessage(failed, GetOperationNameRu(operationId)));

                    if (affectsRecipe)
                    {
                        var snapshot = _snapshotProvider.GetSnapshot();
                        _reasonsSink.SetPolicyReasons(SelectPersistentReasons(snapshot.Reasons));
                    }

                    return failed;
                }

                var decision = _policy.Decide(operationId, result.Reasons);
                switch (decision.Kind)
                {
                    case DecisionKind.BlockedError:
                        _logger.LogWarning("Operation [{Operation}] completed with blocking error reason {ReasonType}", operationId, decision.PrimaryReason?.GetType().Name);
                        _status.ShowError(StatusPresenter.BuildErrorMessage(result, GetOperationNameRu(operationId)));
                        return result.IsFailed ? result : Result.Fail(result.Reasons.OfType<IError>().ToArray());

                    case DecisionKind.BlockedWarning:
                        _logger.LogWarning("Operation [{Operation}] completed with blocking warning reason {ReasonType}", operationId, decision.PrimaryReason?.GetType().Name);
                        _status.ShowWarning(StatusPresenter.BuildWarningMessage(result, GetOperationNameRu(operationId)));
                        break;

                    case DecisionKind.Allowed:
                        var hasWarnings = result.Reasons.OfType<BilingualWarning>().Any();
                        if (hasWarnings)
                        {
                            _status.ShowWarning(StatusPresenter.BuildWarningMessage(result, GetOperationNameRu(operationId)));
                        }
                        else
                        {
                            var kind = GetCompletionMessageKind(operationId);
                            if (kind == CompletionMessageKind.Success && !string.IsNullOrWhiteSpace(successMessage))
                                _status.ShowSuccess(successMessage);
                            else if (kind == CompletionMessageKind.Info && !string.IsNullOrWhiteSpace(successMessage))
                                _status.ShowInfo(successMessage);
                            else
                                _status.Clear();
                        }
                        break;
                }

                if (affectsRecipe && result.IsSuccess)
                {
                    var snapshot = _snapshotProvider.GetSnapshot();
                    _reasonsSink.SetPolicyReasons(SelectPersistentReasons(snapshot.Reasons));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Operation [{Operation}] failed unexpectedly", operationId);
                var error = Result.Fail(new Error("Unexpected operation error").CausedBy(ex));
                _status.ShowError(StatusPresenter.BuildErrorMessage(error, GetOperationNameRu(operationId)));
                return error;
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
        return RunAsync(operationId, operationKind, () => Task.FromResult(execute()), successMessage, affectsRecipe).GetAwaiter().GetResult();
    }

    private static IEnumerable<IReason> SelectPersistentReasons(IEnumerable<IReason> reasons)
    {
        return reasons ?? Enumerable.Empty<IReason>();
    }

    private static string GetOperationNameRu(OperationId id) =>
        id switch
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

    private enum CompletionMessageKind { None, Info, Success }

    private static CompletionMessageKind GetCompletionMessageKind(OperationId id) =>
        id switch
        {
            // Long operations: success
            OperationId.Save => CompletionMessageKind.Success,
            OperationId.Send => CompletionMessageKind.Success,

            // Informational only
            OperationId.Load => CompletionMessageKind.Info,
            OperationId.Receive => CompletionMessageKind.Info,
            OperationId.AddStep => CompletionMessageKind.Info,
            OperationId.RemoveStep => CompletionMessageKind.Info,

            // No message for cell edits
            OperationId.EditCell => CompletionMessageKind.None,

            _ => CompletionMessageKind.Info
        };
}