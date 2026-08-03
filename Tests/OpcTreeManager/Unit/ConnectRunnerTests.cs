using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using NtoLib.OpcTreeManager.Entities;
using NtoLib.OpcTreeManager.TreeOperations;

using Serilog;
using Serilog.Core;
using Serilog.Events;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

/// <summary>
/// Drives <see cref="ConnectRunner.ConnectAll"/> through the <see cref="ConnectCommand"/> delegate
/// seam with fakes, so the connect pass is exercised without a live IProjectHlp. There is no
/// read-back verdict: <c>GetConnections</c> is blind after the structural commit (a connected link
/// reads back as absent), so success is judged by the SCADA tree on reload. These tests assert only
/// what the pass actually knows — every command is issued, in order, a throwing command is caught and
/// counted while the rest still run, and the honest issued/threw tally is returned.
/// </summary>
public sealed class ConnectRunnerTests
{
	[Fact]
	public void ConnectAll_IssuesEveryCommand_InOrder()
	{
		var logger = MakeLogger(new CapturingSink());
		var issued = new List<string>();

		var commands = new[]
		{
			MakeRecordingCommand(LinkTypes.DirectPin, "Root.CH17.Kp$", issued),
			MakeRecordingCommand(LinkTypes.DirectPin, "Root.CH17.StatusWord", issued),
			MakeRecordingCommand(LinkTypes.IConnect, "Root.CH17.Kp", issued),
		};

		var (total, threw) = ConnectRunner.ConnectAll(commands, logger);

		total.Should().Be(3);
		threw.Should().Be(0);
		issued.Should().Equal("Root.CH17.Kp$", "Root.CH17.StatusWord", "Root.CH17.Kp");
	}

	[Fact]
	public void ConnectAll_ThrowingCommand_IsCaughtCountedAndTheRestStillRun()
	{
		var sink = new CapturingSink();
		var logger = MakeLogger(sink);
		var issued = new List<string>();

		var commands = new[]
		{
			MakeRecordingCommand(LinkTypes.IConnect, "Root.A", issued),
			new ConnectCommand(
				LinkType: LinkTypes.IConnect,
				LocalPinPath: "Root.B",
				ExternalPinPath: "Ext.B",
				Connect: () => throw new InvalidOperationException("COM no-op")),
			MakeRecordingCommand(LinkTypes.IConnect, "Root.C", issued),
		};

		var (total, threw) = ConnectRunner.ConnectAll(commands, logger);

		total.Should().Be(3);
		threw.Should().Be(1);

		// The throw does not abort the pass — the command after the throwing one still ran.
		issued.Should().Equal("Root.A", "Root.C");

		// A throw surfaces as exactly one Warning; there is no read-back Error path any more.
		sink.Events.Count(e => e.Level == LogEventLevel.Warning).Should().Be(1);
		sink.Events.Should().NotContain(e => e.Level == LogEventLevel.Error);
	}

	[Fact]
	public void ConnectAll_NoThrows_ReportsZeroThrewAndNoWarning()
	{
		var sink = new CapturingSink();
		var logger = MakeLogger(sink);

		var commands = new[]
		{
			MakeCommand(LinkTypes.IConnect, "Root.CH17.Kp"),
			MakeCommand(LinkTypes.DirectPin, "Root.CH17.Kp$"),
		};

		var (total, threw) = ConnectRunner.ConnectAll(commands, logger);

		total.Should().Be(2);
		threw.Should().Be(0);
		sink.Events.Should().NotContain(e => e.Level == LogEventLevel.Warning);
	}

	[Fact]
	public void ConnectAll_EveryCommandThrows_TallyMatchesTotalAndOneWarningEach()
	{
		var sink = new CapturingSink();
		var logger = MakeLogger(sink);

		var commands = new[]
		{
			MakeThrowingCommand("Root.A"),
			MakeThrowingCommand("Root.B"),
		};

		var (total, threw) = ConnectRunner.ConnectAll(commands, logger);

		total.Should().Be(2);
		threw.Should().Be(2);
		sink.Events.Count(e => e.Level == LogEventLevel.Warning).Should().Be(2);
	}

	[Fact]
	public void ConnectAll_EmptyCommandSet_ReturnsZeroTally()
	{
		var logger = MakeLogger(new CapturingSink());

		var (total, threw) = ConnectRunner.ConnectAll(Array.Empty<ConnectCommand>(), logger);

		total.Should().Be(0);
		threw.Should().Be(0);
	}

	private static ConnectCommand MakeCommand(string linkType, string localPinPath)
	{
		return new ConnectCommand(
			LinkType: linkType,
			LocalPinPath: localPinPath,
			ExternalPinPath: "Ext." + localPinPath,
			Connect: () => { });
	}

	private static ConnectCommand MakeRecordingCommand(string linkType, string localPinPath, List<string> issued)
	{
		return new ConnectCommand(
			LinkType: linkType,
			LocalPinPath: localPinPath,
			ExternalPinPath: "Ext." + localPinPath,
			Connect: () => issued.Add(localPinPath));
	}

	private static ConnectCommand MakeThrowingCommand(string localPinPath)
	{
		return new ConnectCommand(
			LinkType: LinkTypes.IConnect,
			LocalPinPath: localPinPath,
			ExternalPinPath: "Ext." + localPinPath,
			Connect: () => throw new InvalidOperationException("COM no-op"));
	}

	private static Logger MakeLogger(CapturingSink sink)
	{
		return new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.WriteTo.Sink(sink)
			.CreateLogger();
	}

	private sealed class CapturingSink : ILogEventSink
	{
		public List<LogEvent> Events { get; } = new();

		public void Emit(LogEvent logEvent)
		{
			Events.Add(logEvent);
		}
	}
}
