using System;
using System.Windows.Forms;

using MasterSCADA.Hlp;

using NtoLib.OpcTreeManager.Entities;

using Serilog;
using Serilog.Core;

namespace NtoLib.OpcTreeManager.TreeOperations;

internal static class DeferredExecutor
{
	private const int MaxRetries = 100;
	private const int RetryIntervalMs = 200;
	private const double TotalTimeoutSeconds = MaxRetries * RetryIntervalMs / 1000.0;

	/// <summary>
	/// Posts a single-shot deferred execution that runs
	/// <see cref="PlanExecutor.Execute(RebuildPlan)"/> on the first timer tick after
	/// <see cref="IProjectHlp.InRuntime"/> drops to <c>false</c>. The caller transfers
	/// ownership of <paramref name="logger"/> — it is disposed when execution finishes
	/// (success, failure, or timeout). <paramref name="onFinished"/> is invoked in a
	/// finally block after execution (or abort) to let the caller clear its pending
	/// plan state.
	/// </summary>
	public static void Post(
		PlanExecutor executor,
		RebuildPlan plan,
		Logger? logger,
		IProjectHlp project,
		Action onFinished)
	{
		var log = logger?.ForContext(typeof(DeferredExecutor));
		var timer = new Timer { Interval = RetryIntervalMs };
		var retries = MaxRetries;

		log?.Debug(
			"Deferred execution posted; waiting for InRuntime=false (max {MaxRetries} × {IntervalMs}ms)",
			MaxRetries, RetryIntervalMs);

		timer.Tick += (_, _) =>
		{
			if (project.InRuntime)
			{
				retries--;

				if (retries > 0)
				{
					return;
				}

				AbortWithTimeout(timer, logger, log, onFinished);
				return;
			}

			FinishTimer(timer);

			try
			{
				log?.Debug("InRuntime=false; executing plan");
				var result = executor.Execute(plan);

				if (result.IsFailed)
				{
					var errorMessage = string.Join("; ", result.Errors);
					log?.Error("Deferred execution failed: {ErrorMessage}", errorMessage);
				}
				else
				{
					log?.Information("Deferred execution completed successfully");
				}
			}
			catch (Exception exception)
			{
				log?.Error(exception, "Deferred execution failed with exception");
			}
			finally
			{
				logger?.Dispose();
				onFinished();
			}
		};

		timer.Start();
	}

	private static void AbortWithTimeout(Timer timer, Logger? logger, ILogger? log, Action onFinished)
	{
		FinishTimer(timer);

		try
		{
			log?.Error(
				"Deferred execution aborted: IProjectHlp.InRuntime is still true after {MaxRetries} retries ({TotalSeconds}s)",
				MaxRetries,
				TotalTimeoutSeconds);
		}
		finally
		{
			logger?.Dispose();
			onFinished();
		}
	}

	/// <summary>
	/// Stops and disposes the timer, swallowing any exception from <c>Dispose</c> so it
	/// does not escape onto the STA message pump. Timer cleanup is best-effort — the
	/// real work runs inside its own try/finally.
	/// </summary>
	private static void FinishTimer(Timer timer)
	{
		try
		{
			timer.Stop();
			timer.Dispose();
		}
		catch
		{
			// intentionally swallowed
		}
	}
}
