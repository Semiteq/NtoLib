using System;
using System.Windows.Forms;

using MasterSCADA.Hlp;

using NtoLib.OpcTreeManager.Entities;

using Serilog;
using Serilog.Core;

namespace NtoLib.OpcTreeManager.TreeOperations;

internal static class DeferredExecutor
{
	private const int MaxPolls = 100;
	private const int RetryIntervalMs = 200;
	private const double TotalTimeoutSeconds = MaxPolls * RetryIntervalMs / 1000.0;

	/// <summary>
	/// Posts a single-tick deferred execution. On the first timer tick after
	/// <see cref="IProjectHlp.InRuntime"/> drops to <c>false</c> it runs
	/// <see cref="PlanExecutor.Execute(RebuildPlan)"/> — which connects every link
	/// in-pass — and then finishes. The <c>InRuntime==false</c> wait is required because a
	/// deferred-execution FB may not mutate the tree while the host is still in runtime
	/// (see the deferred-execution known issue).
	/// <para>
	/// Owns <paramref name="logger"/> from here on (see <c>OpcTreeManagerFB.FlushPendingPlan</c> for
	/// the ownership transfer); disposes it and calls <paramref name="onFinished"/> exactly once on
	/// every terminal path via a single guarded release.
	/// </para>
	/// </summary>
	public static void Post(
		PlanExecutor executor,
		RebuildPlan plan,
		Logger? logger,
		IProjectHlp project,
		Action onFinished)
	{
		var log = logger?.ForContext(typeof(DeferredExecutor));

		// Once-only release: disposes the logger and calls onFinished exactly once, whichever
		// terminal branch reaches it first. WinForms timers tick on the STA pump (single thread),
		// so a plain bool guard is sufficient — no locking needed.
		var released = false;

		void Release()
		{
			if (released)
			{
				return;
			}

			released = true;
			logger?.Dispose();
			onFinished();
		}

		var timer = new Timer { Interval = RetryIntervalMs };
		var polls = MaxPolls;

		log?.Debug(
			"Deferred execution posted; waiting for InRuntime=false (max {MaxPolls} × {IntervalMs}ms)",
			MaxPolls, RetryIntervalMs);

		// The whole tick body runs inside RunTickGuarded so any escape stops the timer and releases once.
		timer.Tick += (_, _) => RunTickGuarded(timer, log, Release, () =>
		{
			if (project.InRuntime)
			{
				polls--;

				if (polls > 0)
				{
					return;
				}

				AbortWithTimeout(timer, log, Release);
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
				Release();
			}
		});

		timer.Start();
	}

	/// <summary>
	/// Runs a tick body and guarantees that ANY escape from it stops+disposes the timer and calls
	/// <paramref name="release"/> exactly once (<paramref name="release"/> carries its own idempotency
	/// guard). A normal return does nothing, so a body that keeps the timer running just returns.
	/// </summary>
	private static void RunTickGuarded(Timer timer, ILogger? log, Action release, Action body)
	{
		try
		{
			body();
		}
		catch (Exception exception)
		{
			log?.Error(exception, "Deferred execution tick handler threw; stopping timer and releasing");
			FinishTimer(timer);
			release();
		}
	}

	private static void AbortWithTimeout(
		Timer timer,
		ILogger? log,
		Action release)
	{
		FinishTimer(timer);

		try
		{
			log?.Error(
				"Deferred execution aborted: IProjectHlp.InRuntime is still true after {MaxPolls} polls ({TotalSeconds}s)",
				MaxPolls,
				TotalTimeoutSeconds);
		}
		finally
		{
			release();
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
