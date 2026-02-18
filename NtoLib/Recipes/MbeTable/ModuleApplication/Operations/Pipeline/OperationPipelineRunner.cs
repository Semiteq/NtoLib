using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;
using NtoLib.Recipes.MbeTable.ModuleApplication.Policy;
using NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleApplication.Status;
using NtoLib.Recipes.MbeTable.ModuleCore.Snapshot;
using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;

/// <summary>
/// Runs any recipe operation through the four cross-cutting concerns:
/// permission check, concurrency gate, status presentation, and post-success effects.
/// Knows nothing about CSV, Modbus, clipboard, or any specific domain operation.
/// </summary>
public sealed class OperationPipelineRunner
{
	private readonly IStateProvider _state;
	private readonly PolicyEngine _policy;
	private readonly IStatusPresenter _status;
	private readonly ILogger<OperationPipelineRunner> _logger;

	public OperationPipelineRunner(
		IStateProvider state,
		PolicyEngine policy,
		IStatusPresenter status,
		ILogger<OperationPipelineRunner> logger)
	{
		_state = state ?? throw new ArgumentNullException(nameof(state));
		_policy = policy ?? throw new ArgumentNullException(nameof(policy));
		_status = status ?? throw new ArgumentNullException(nameof(status));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public Task<Result> RunAsync(
		OperationMetadata op,
		Func<Task<Result>> execute,
		string? successMessage)
	{
		return RunCoreAsync(op, async () =>
		{
			var result = await execute().ConfigureAwait(false);
			return new OperationOutcome(result, MergeReasons(result));
		}, successMessage);
	}

	public Task<Result> RunAsync<T>(
		OperationMetadata op,
		Func<Task<Result<T>>> execute,
		string? successMessage)
	{
		return RunCoreAsync(op, async () =>
		{
			var result = await execute().ConfigureAwait(false);
			return new OperationOutcome(result.ToResult(), MergeReasons(result));
		}, successMessage);
	}

	public Result RunSync<T>(
		OperationMetadata op,
		Func<Result<T>> execute,
		string? successMessage)
	{
		return RunCoreAsync(op, () =>
		{
			var result = execute();
			return Task.FromResult(new OperationOutcome(result.ToResult(), MergeReasons(result)));
		}, successMessage).GetAwaiter().GetResult();
	}

	private async Task<Result> RunCoreAsync(
		OperationMetadata op,
		Func<Task<OperationOutcome>> execute,
		string? successMessage)
	{
		var decision = _state.Evaluate(op.Id);
		if (decision.Kind != DecisionKind.Allowed)
		{
			_logger.LogInformation("Operation [{Operation}] blocked before start", op.Id);
			ShowBlocked(decision, op);
			return ToBlockedResult(decision);
		}

		var gateResult = AcquireGate(op);
		if (gateResult.IsFailed)
		{
			_logger.LogInformation("Operation [{Operation}] blocked by concurrency gate", op.Id);
			ShowError(gateResult.ToResult(), op);
			return gateResult.ToResult();
		}

		using (gateResult.Value)
		{
			try
			{
				_status.Clear();

				var outcome = await execute().ConfigureAwait(false);

				if (ContainsErrors(outcome.Reasons))
				{
					_logger.LogWarning(
						"Operation [{Operation}] finished with error reasons, forcing failure", op.Id);
					ShowError(outcome.BaseResult, op, outcome.Reasons);
					return BuildFailedResult(outcome.Reasons);
				}

				PresentCompletion(op, outcome.BaseResult, outcome.Reasons, successMessage);

				if (outcome.BaseResult.IsSuccess)
					ApplyPostSuccessEffects(op, outcome.Reasons);

				return outcome.BaseResult;
			}
			catch (Exception ex)
			{
				return HandleUnexpectedException(ex, op);
			}
		}
	}

	private Result<IDisposable> AcquireGate(OperationMetadata op)
	{
		return op.IsLongRunning
			? _state.BeginOperation(op.Kind, op.Id)
			: Result.Ok<IDisposable>(NullDisposable.Instance);
	}

	private void PresentCompletion(
		OperationMetadata op,
		Result baseResult,
		IReadOnlyList<IReason> reasons,
		string? successMessage)
	{
		var decision = _policy.Decide(op.Id, reasons);

		if (decision.Kind == DecisionKind.BlockedError)
		{
			ShowError(baseResult, op, reasons);
			return;
		}

		if (decision.Kind == DecisionKind.BlockedWarning)
		{
			ShowWarning(baseResult, op, reasons);
			return;
		}

		var hasWarnings = reasons.OfType<BilingualWarning>().Any();
		if (hasWarnings)
		{
			ShowWarning(baseResult, op, reasons);
			return;
		}

		ShowSuccessIfNeeded(successMessage, op);
	}

	private void ShowBlocked(OperationDecision decision, OperationMetadata op)
	{
		if (decision.Kind == DecisionKind.BlockedWarning)
		{
			var r = Result.Ok();
			if (decision.PrimaryReason != null)
				r = r.WithReason(decision.PrimaryReason);
			ShowWarning(r, op);
			return;
		}

		if (decision.Kind == DecisionKind.BlockedError)
		{
			Result r;
			if (decision.PrimaryReason is IError err)
			{
				r = Result.Fail(err);
			}
			else
			{
				r = Result.Fail(new ApplicationInvalidOperationError("Operation not allowed"));
				if (decision.PrimaryReason != null)
					r = r.WithReason(decision.PrimaryReason);
			}

			ShowError(r, op);
			return;
		}

		_status.Clear();
	}

	private void ShowError(Result result, OperationMetadata op, IReadOnlyList<IReason>? reasons = null)
	{
		var msg = StatusPresenter.BuildErrorMessage(result, op.DisplayNameRu, reasons);
		_status.ShowError(msg);
	}

	private void ShowWarning(Result result, OperationMetadata op, IReadOnlyList<IReason>? reasons = null)
	{
		var msg = StatusPresenter.BuildWarningMessage(result, op.DisplayNameRu, reasons);
		_status.ShowWarning(msg);
	}

	private void ShowSuccessIfNeeded(string? successMessage, OperationMetadata op)
	{
		if (op.CompletionMessage == CompletionMessageKind.Success && !string.IsNullOrWhiteSpace(successMessage))
		{
			_status.ShowSuccess(successMessage!);
			return;
		}

		if (op.CompletionMessage == CompletionMessageKind.Info && !string.IsNullOrWhiteSpace(successMessage))
		{
			_status.ShowInfo(successMessage!);
			return;
		}

		_status.Clear();
	}

	private void ApplyPostSuccessEffects(OperationMetadata op, IReadOnlyList<IReason> reasons)
	{
		if (op.UpdatesPolicyReasons)
			_state.SetPolicyReasons(reasons);

		switch (op.ConsistencyEffect)
		{
			case ConsistencyEffect.MarkConsistent:
				_state.SetRecipeConsistent(true);
				break;
			case ConsistencyEffect.MarkInconsistent:
				_state.SetRecipeConsistent(false);
				break;
		}
	}

	private Result HandleUnexpectedException(Exception ex, OperationMetadata op)
	{
		_logger.LogError(ex, "Operation [{Operation}] failed unexpectedly", op.DisplayNameRu);
		var error = new ApplicationUnexpectedOperationError(ex.Message).CausedBy(ex);
		var msg = StatusPresenter.BuildErrorMessage(error, op.DisplayNameRu);
		_status.ShowError(msg);
		return error;
	}

	private static IReadOnlyList<IReason> MergeReasons(Result result)
	{
		return (IReadOnlyList<IReason>?)result.Reasons ?? Array.Empty<IReason>();
	}

	private static IReadOnlyList<IReason> MergeReasons<T>(Result<T> result)
	{
		var own = result.Reasons ?? (IReadOnlyList<IReason>)Array.Empty<IReason>();

		if (result.IsSuccess && result.Value is RecipeAnalysisSnapshot snapshot)
			return own.Concat(snapshot.Reasons).ToList();

		return own is List<IReason> list ? list : own.ToList();
	}

	private static bool ContainsErrors(IReadOnlyList<IReason> reasons)
	{
		for (var i = 0; i < reasons.Count; i++)
		{
			if (reasons[i] is IError)
				return true;
		}
		return false;
	}

	private static Result BuildFailedResult(IReadOnlyList<IReason> reasons)
	{
		var errors = reasons.OfType<IError>().ToArray();
		return Result.Fail(errors);
	}

	private static Result ToBlockedResult(OperationDecision decision)
	{
		if (decision.Kind == DecisionKind.BlockedError)
		{
			if (decision.PrimaryReason is IError err)
				return Result.Fail(err);

			var res = Result.Fail(new ApplicationInvalidOperationError("Operation not allowed"));
			return decision.PrimaryReason != null ? res.WithReason(decision.PrimaryReason) : res;
		}

		var ok = Result.Ok();
		return decision.PrimaryReason != null ? ok.WithReason(decision.PrimaryReason) : ok;
	}

	/// <summary>
	/// Bundles the result and its merged reasons from a single operation execution.
	/// </summary>
	private readonly struct OperationOutcome
	{
		public Result BaseResult { get; }
		public IReadOnlyList<IReason> Reasons { get; }

		public OperationOutcome(Result baseResult, IReadOnlyList<IReason> reasons)
		{
			BaseResult = baseResult;
			Reasons = reasons;
		}
	}

	private sealed class NullDisposable : IDisposable
	{
		public static readonly NullDisposable Instance = new();
		public void Dispose() { }
	}
}
