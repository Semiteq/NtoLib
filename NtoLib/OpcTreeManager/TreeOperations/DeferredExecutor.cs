using System;
using System.Linq;
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
	/// <see cref="PlanExecutor.Execute(RebuildPlan, Action)"/>, which connects every link
	/// in-pass, and then finishes. The <c>InRuntime==false</c> wait is required because a
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

		logger?.Information(
			"Rebuild of group '{GroupName}' queued; waits for the host to leave runtime, "
			+ "timeout {TimeoutSeconds}s",
			plan.GroupName, TotalTimeoutSeconds);

		// The whole tick body runs inside RunTickGuarded so any escape stops the timer and releases once.
		timer.Tick += (_, _) => RunTickGuarded(timer, logger, plan.GroupName, Release, () =>
		{
			if (project.InRuntime)
			{
				polls--;

				if (polls > 0)
				{
					return;
				}

				AbortWithTimeout(timer, logger, plan.GroupName, Release);
				return;
			}

			FinishTimer(timer);

			var treeChanged = false;

			try
			{
				var result = executor.Execute(plan, () => treeChanged = true);

				if (result.IsFailed)
				{
					var reason = string.Join("; ", result.Errors.Select(error => error.Message));
					LogRebuildFailed(logger, plan.GroupName, treeChanged, reason, exception: null);
				}
			}
			catch (Exception exception)
			{
				LogRebuildFailed(logger, plan.GroupName, treeChanged, exception.Message, exception);
			}
			finally
			{
				Release();
			}
		});

		timer.Start();
	}

	private static void LogRebuildFailed(
		ILogger? logger,
		string groupName,
		bool treeChanged,
		string reason,
		Exception? exception)
	{
		var template = treeChanged
			? "Rebuild of group '{GroupName}' failed after the reshape began; the tree is partially "
				+ "changed, close the project without saving: {Reason}"
			: "Rebuild of group '{GroupName}' not started, the tree is unchanged: {Reason}";

		if (exception == null)
		{
			logger?.Error(template, groupName, reason);
			return;
		}

		logger?.Error(exception, template, groupName, reason);
	}

	/// <summary>
	/// Runs a tick body and guarantees that ANY escape from it stops+disposes the timer and calls
	/// <paramref name="release"/> exactly once (<paramref name="release"/> carries its own idempotency
	/// guard). A normal return does nothing, so a body that keeps the timer running just returns.
	/// </summary>
	private static void RunTickGuarded(Timer timer, ILogger? logger, string groupName, Action release, Action body)
	{
		try
		{
			body();
		}
		catch (Exception exception)
		{
			logger?.Error(exception, "Rebuild of group '{GroupName}' abandoned", groupName);
			FinishTimer(timer);
			release();
		}
	}

	private static void AbortWithTimeout(
		Timer timer,
		ILogger? logger,
		string groupName,
		Action release)
	{
		FinishTimer(timer);

		try
		{
			logger?.Error(
				"Rebuild of group '{GroupName}' not started: the host stayed in runtime "
				+ "{TimeoutSeconds}s after ToDesign; the plan is discarded",
				groupName, TotalTimeoutSeconds);
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
