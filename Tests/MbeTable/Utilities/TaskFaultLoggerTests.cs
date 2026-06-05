using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Utilities;

using Xunit;

namespace Tests.MbeTable.Utilities;

public sealed class TaskFaultLoggerTests
{
	[Fact]
	public async Task LogOnFault_FaultedTask_LogsErrorOnce()
	{
		var logger = new RecordingLogger();
		var faulted = Task.FromException(new InvalidOperationException("boom"));

		await TaskFaultLogger.LogOnFault(faulted, logger, "read faulted");

		logger.Entries.Should().ContainSingle()
			.Which.Should().BeEquivalentTo(new
			{
				Level = LogLevel.Error,
				Message = "read faulted"
			});
		logger.Entries[0].Exception.Should().BeOfType<InvalidOperationException>()
			.Which.Message.Should().Be("boom");
	}

	[Fact]
	public async Task LogOnFault_CompletedTask_DoesNotLog()
	{
		var logger = new RecordingLogger();
		var completed = Task.CompletedTask;

		await AwaitSkippedContinuation(TaskFaultLogger.LogOnFault(completed, logger, "read faulted"));

		logger.Entries.Should().BeEmpty();
	}

	[Fact]
	public async Task LogOnFault_CanceledTask_DoesNotLog()
	{
		var logger = new RecordingLogger();
		var canceled = Task.FromCanceled(new CancellationToken(true));

		await AwaitSkippedContinuation(TaskFaultLogger.LogOnFault(canceled, logger, "read faulted"));

		logger.Entries.Should().BeEmpty();
	}

	// An OnlyOnFaulted continuation on a non-faulted antecedent never runs; the TPL transitions
	// it to Canceled. Awaiting that cancellation is the deterministic signal that the body was
	// skipped, replacing any timing-based polling.
	private static async Task AwaitSkippedContinuation(Task continuation)
	{
		var act = async () => await continuation;
		await act.Should().ThrowAsync<TaskCanceledException>();
	}

	private sealed class RecordingLogger : ILogger
	{
		private readonly List<LogEntry> _entries = new();

		public IReadOnlyList<LogEntry> Entries => _entries;

		public IDisposable BeginScope<TState>(TState state) where TState : notnull
		{
			return NullScope.Instance;
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return true;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			_entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
		}
	}

	private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

	private sealed class NullScope : IDisposable
	{
		public static readonly NullScope Instance = new();

		private NullScope()
		{
		}

		public void Dispose()
		{
		}
	}
}
