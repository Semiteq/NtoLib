using FluentAssertions;

using MasterSCADALib;

using NtoLib.OpcTreeManager.TreeOperations;

using Serilog;
using Serilog.Core;
using Serilog.Events;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

public sealed class LinkCollectorTests
{
	[Fact]
	public void BuildLinks_IConnectAndDollarDirectPinToSameExternal_KeepsBoth()
	{
		// The Kp/Ti/Td pattern seen in the live tree: the base pin holds the iconnect (feedback)
		// and its $ sibling holds a directPin (input), BOTH to the same external element. These are
		// two distinct wires, not two halves of one — the directPin input is what reconnects
		// reliably. Folding them (the old behavior) dropped the input and left the pin unconnectable.
		var pins = new[]
		{
			FakePin.WithIConnect("Root.Kp", "Plant.Kp"),
			FakePin.WithDirectPin("Root.Kp$", "Plant.Kp"),
		};

		var result = LinkCollector.BuildLinks(pins, log: null);

		result.Should().HaveCount(2);
		result.Should().ContainSingle(x =>
			x.LocalPinPath == "Root.Kp" && x.ExternalPinPath == "Plant.Kp" && x.LinkType == "iconnect");
		result.Should().ContainSingle(x =>
			x.LocalPinPath == "Root.Kp$" && x.ExternalPinPath == "Plant.Kp" && x.LinkType == "directPin");
	}

	[Fact]
	public void BuildLinks_ExactDuplicateRows_CollapsedToOne()
	{
		// The only rows dedup removes: identical (local, external, linkType) triples produced when
		// one pin matches more than one enumerated mask and the same wire surfaces twice.
		var pin = FakePin.FromMaskMap(
			"Root.Signal",
			new Dictionary<EConnectionTypeMask, string[]>
			{
				[EConnectionTypeMask.ctGenericPin] = new[] { "Producer.Output", "Producer.Output" },
			});

		var result = LinkCollector.BuildLinks(new[] { pin }, log: null);

		result.Should().HaveCount(1);
		result[0].LocalPinPath.Should().Be("Root.Signal");
		result[0].ExternalPinPath.Should().Be("Producer.Output");
		result[0].LinkType.Should().Be("directPin");
	}

	[Fact]
	public void BuildLinks_PureDirectPout_EmitsOneDirectPoutRow()
	{
		var pins = new[]
		{
			FakePin.WithDirectPout("Root.Signal", "Consumer.Input"),
		};

		var result = LinkCollector.BuildLinks(pins, log: null);

		result.Should().HaveCount(1);
		result[0].LinkType.Should().Be("directPout");
		result[0].ExternalPinPath.Should().Be("Consumer.Input");
	}

	[Fact]
	public void BuildLinks_PureDirectPin_EmitsOneDirectPinRow()
	{
		var pins = new[]
		{
			FakePin.WithDirectPin("Root.Input", "Producer.Output"),
		};

		var result = LinkCollector.BuildLinks(pins, log: null);

		result.Should().HaveCount(1);
		result[0].LinkType.Should().Be("directPin");
		result[0].ExternalPinPath.Should().Be("Producer.Output");
	}

	[Fact]
	public void BuildLinks_MixedDirectPinAndIConnectOnSamePin_EmitsOneRowPerLinkType()
	{
		var pin = FakePin.FromMaskMap(
			"Root.Mixed",
			new Dictionary<EConnectionTypeMask, string[]>
			{
				[EConnectionTypeMask.ctGenericPin] = new[] { "Producer.Output" },
				[EConnectionTypeMask.ctIConnect] = new[] { "Sibling.Twin" },
			});

		var result = LinkCollector.BuildLinks(new[] { pin }, log: null);

		result.Should().HaveCount(2);
		result.Should().ContainSingle(x =>
			x.LinkType == "directPin" && x.ExternalPinPath == "Producer.Output");
		result.Should().ContainSingle(x =>
			x.LinkType == "iconnect" && x.ExternalPinPath == "Sibling.Twin");
	}

	[Fact]
	public void BuildLinks_DirectPinAndIConnectOnSamePinToSameExternal_KeepsBoth()
	{
		// The Kp/Ti/Td case: one pin carries BOTH an iconnect (feedback) AND a directPin (input)
		// to the SAME external element. These are two distinct wires on one pin, not a $-twin, so
		// both must survive. Keying by (local, external) alone dropped the input half of every
		// iconnect pin, so the snapshot restored an incomplete connection.
		var pin = FakePin.FromMaskMap(
			"Root.Kp",
			new Dictionary<EConnectionTypeMask, string[]>
			{
				[EConnectionTypeMask.ctGenericPin] = new[] { "Plant.Kp" },
				[EConnectionTypeMask.ctIConnect] = new[] { "Plant.Kp" },
			});

		var result = LinkCollector.BuildLinks(new[] { pin }, log: null);

		result.Should().HaveCount(2);
		result.Should().ContainSingle(x =>
			x.LinkType == "directPin" && x.LocalPinPath == "Root.Kp" && x.ExternalPinPath == "Plant.Kp");
		result.Should().ContainSingle(x =>
			x.LinkType == "iconnect" && x.LocalPinPath == "Root.Kp" && x.ExternalPinPath == "Plant.Kp");
	}

	[Fact]
	public void BuildLinks_DollarOnlyPin_IsKept()
	{
		// Command-pin pattern: the wire surfaces only on the $ sibling under
		// ctGenericPin. PlanExecutor replays this row via the no-arg Connect
		// overload, which auto-routes POUT↔POUT pairs to IConnect.
		var pins = new[]
		{
			FakePin.WithDirectPin("Root.Orphan$", "Producer.Output"),
		};

		var result = LinkCollector.BuildLinks(pins, log: null);

		result.Should().HaveCount(1);
		result[0].LocalPinPath.Should().Be("Root.Orphan$");
		result[0].LinkType.Should().Be("directPin");
	}

	[Fact]
	public void BuildLinks_SiblingsWithDifferentExternals_BothKept()
	{
		// Independent wires: each sibling points at a different external pin.
		// Dedup keys differ, so nothing collapses.
		var pins = new[]
		{
			FakePin.WithIConnect("Root.Pin", "External.A"),
			FakePin.WithDirectPin("Root.Pin$", "External.B"),
		};

		var result = LinkCollector.BuildLinks(pins, log: null);

		result.Should().HaveCount(2);
		result.Should().ContainSingle(x =>
			x.LocalPinPath == "Root.Pin" && x.ExternalPinPath == "External.A" && x.LinkType == "iconnect");
		result.Should().ContainSingle(x =>
			x.LocalPinPath == "Root.Pin$" && x.ExternalPinPath == "External.B" && x.LinkType == "directPin");
	}

	[Fact]
	public void BuildLinks_ExactDuplicateRows_LogsWarningNamingDroppedTriple()
	{
		var pin = FakePin.FromMaskMap(
			"Root.Signal",
			new Dictionary<EConnectionTypeMask, string[]>
			{
				[EConnectionTypeMask.ctGenericPin] = new[] { "Producer.Output", "Producer.Output" },
			});

		var sink = new CapturingSink();
		var logger = BuildLogger(sink);

		LinkCollector.BuildLinks(new[] { pin }, logger);

		sink.Warnings.Should().ContainSingle(m =>
			m.Contains("Root.Signal")
			&& m.Contains("Producer.Output")
			&& m.Contains("directPin")
			&& m.Contains("dropped"));
	}

	[Fact]
	public void BuildLinks_IConnectWithMatchingDollarTwin_Silent()
	{
		// Settings pin: iconnect (feedback) and directPin (input) both captured to the SAME external.
		// Both halves present — the check says nothing (no Warning, no Debug).
		var pins = new[]
		{
			FakePin.WithIConnect("Root.Kp", "Plant.Kp"),
			FakePin.WithDirectPin("Root.Kp$", "Plant.Kp"),
		};

		var sink = new CapturingSink();
		var logger = BuildLogger(sink);

		LinkCollector.BuildLinks(pins, logger);

		sink.Warnings.Should().NotContain(m => m.Contains("Capture check"));
		sink.Debugs.Should().NotContain(m => m.Contains("Feedback-only"));
	}

	[Fact]
	public void BuildLinks_IConnectWithNoInputSiblingRow_LogsWarning()
	{
		// The only shape a dropped/blind capture takes: an iconnect whose $ input sibling has NO
		// captured row at all. This is what the old $-twin fold silently dropped, and what read-back
		// blindness (known-issue 11) produces. It must surface at Warning naming the pin + the sibling.
		var pins = new[]
		{
			FakePin.WithIConnect("Root.Setpoint", "Plant.TemperatureSP"),
		};

		var sink = new CapturingSink();
		var logger = BuildLogger(sink);

		LinkCollector.BuildLinks(pins, logger);

		sink.Warnings.Should().ContainSingle(m =>
			m.Contains("Capture check")
			&& m.Contains("Root.Setpoint")
			&& m.Contains("Plant.TemperatureSP")
			&& m.Contains("no captured link"));
	}

	[Fact]
	public void BuildLinks_IConnectWithDollarTwinToDifferentExternal_LogsDebugNotWarning()
	{
		// The Setpoint feedback-only case: the $ directPin sibling IS captured, but routes to a
		// DIFFERENT external (Setpoint$ → …Setpoints.Setpoint) than the iconnect (Setpoint →
		// …TemperatureSP). The input half is present, so this is the expected PinPout shape — Debug,
		// NOT a Warning.
		var pins = new[]
		{
			FakePin.WithIConnect("Root.Setpoint", "Plant.TemperatureSP"),
			FakePin.WithDirectPin("Root.Setpoint$", "Plant.Setpoints.Setpoint"),
		};

		var sink = new CapturingSink();
		var logger = BuildLogger(sink);

		LinkCollector.BuildLinks(pins, logger);

		sink.Warnings.Should().NotContain(m => m.Contains("Capture check"));
		sink.Debugs.Should().ContainSingle(m =>
			m.Contains("Feedback-only")
			&& m.Contains("Root.Setpoint")
			&& m.Contains("Plant.TemperatureSP"));
	}

	private static ILogger BuildLogger(CapturingSink sink)
	{
		return new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.WriteTo.Sink(sink)
			.CreateLogger();
	}

	private sealed class CapturingSink : ILogEventSink
	{
		public List<string> Warnings { get; } = new();
		public List<string> Debugs { get; } = new();

		public void Emit(LogEvent logEvent)
		{
			if (logEvent.Level == LogEventLevel.Warning)
			{
				Warnings.Add(logEvent.RenderMessage());
			}
			else if (logEvent.Level == LogEventLevel.Debug)
			{
				Debugs.Add(logEvent.RenderMessage());
			}
		}
	}

	private static class FakePin
	{
		public static PinView WithDirectPin(string fullName, params string[] peers)
		{
			return Build(fullName, EConnectionTypeMask.ctGenericPin, peers);
		}

		public static PinView WithDirectPout(string fullName, params string[] peers)
		{
			return Build(fullName, EConnectionTypeMask.ctGenericPout, peers);
		}

		public static PinView WithIConnect(string fullName, params string[] peers)
		{
			return Build(fullName, EConnectionTypeMask.ctIConnect, peers);
		}

		public static PinView FromMaskMap(string fullName, IReadOnlyDictionary<EConnectionTypeMask, string[]> peersByMask)
		{
			return new PinView(
				Name: ExtractShortName(fullName),
				FullName: fullName,
				GetConnections: mask => peersByMask.TryGetValue(mask, out var peers)
					? peers
					: Array.Empty<string>());
		}

		private static PinView Build(string fullName, EConnectionTypeMask activeMask, string[] peers)
		{
			return new PinView(
				Name: ExtractShortName(fullName),
				FullName: fullName,
				GetConnections: mask => mask == activeMask ? peers : Array.Empty<string>());
		}

		private static string ExtractShortName(string fullName)
		{
			var lastDot = fullName.LastIndexOf('.');
			return lastDot < 0 ? fullName : fullName.Substring(lastDot + 1);
		}
	}
}
