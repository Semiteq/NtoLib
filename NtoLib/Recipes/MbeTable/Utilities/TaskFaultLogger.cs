using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace NtoLib.Recipes.MbeTable.Utilities;

/// <summary>
/// Attaches a fault-logging continuation to a fire-and-forget <see cref="Task"/>, so an
/// unobserved exception is logged instead of silently discarded. The continuation runs only
/// on the faulted branch; completed and canceled tasks are left untouched.
/// </summary>
internal static class TaskFaultLogger
{
	/// <summary>
	/// Returns the continuation task so callers (notably tests) can await it deterministically.
	/// Production call sites ignore the return value.
	/// </summary>
	public static Task LogOnFault(Task task, ILogger? logger, string message)
	{
		return task.ContinueWith(
			faultedTask => logger?.LogError(Unwrap(faultedTask.Exception), message),
			CancellationToken.None,
			TaskContinuationOptions.OnlyOnFaulted,
			TaskScheduler.Default);
	}

	private static Exception? Unwrap(AggregateException? exception)
	{
		return exception?.InnerException ?? exception;
	}
}
