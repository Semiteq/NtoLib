using System;

using FluentAssertions;

using MasterSCADALib;

using NtoLib.OpcTreeManager.TreeOperations;

using Serilog.Events;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

public sealed class LinkCollectorTests
{
	[Fact]
	public void BuildLinks_IConnectAndDollarDirectPinToSameExternal_KeepsBoth()
	{
		// Base pin holds the iconnect, its $ sibling the directPin, both to the same external: two
		// distinct wires. See Docs/known_issues/11-opc-pinpout-sibling-and-iconnect-connect.md.
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
	public void BuildLinks_ExactDuplicateRows_CollapsedToOneWithNoWarning()
	{
		// The only rows dedup removes: identical (local, external, linkType) triples produced when
		// one pin matches more than one enumerated mask and the same wire surfaces twice.
		var pin = FakePin.FromMaskMap(
			"Root.Signal",
			new Dictionary<EConnectionTypeMask, string[]>
			{
				[EConnectionTypeMask.ctGenericPin] = new[] { "Producer.Output", "Producer.Output" },
			});

		var sink = new CapturingSink();

		var result = LinkCollector.BuildLinks(new[] { pin }, sink.ToLogger());

		result.Should().HaveCount(1);
		result[0].LocalPinPath.Should().Be("Root.Signal");
		result[0].ExternalPinPath.Should().Be("Producer.Output");
		result[0].LinkType.Should().Be("directPin");
		sink.Events.Should().NotContain(e => e.Level == LogEventLevel.Warning);
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
		// One pin carries both an iconnect and a directPin to the same external: two wires, not a
		// $-twin. See Docs/known_issues/11-opc-pinpout-sibling-and-iconnect-connect.md.
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
		// overload, which auto-routes POUT<->POUT pairs to IConnect.
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
	public void CollectLinks_LogsThePerNodeTallyAtInformation()
	{
		var pins = new[]
		{
			FakePin.WithIConnect("Root.CBr4.Kp", "Plant.Kp"),
			FakePin.WithDirectPin("Root.CBr4.Kp$", "Plant.Kp"),
		};

		var sink = new CapturingSink();

		var result = LinkCollector.CollectLinks("Root.CBr4", pins, sink.ToLogger());

		result.Should().HaveCount(2);

		var tally = sink.Events.Single(e =>
			e.MessageTemplate.Text == "Captured '{NodePath}': {LinkCount} links");

		tally.Level.Should().Be(LogEventLevel.Information);
		tally.Properties["NodePath"].ToString().Trim('"').Should().Be("Root.CBr4");
		tally.Properties["LinkCount"].ToString().Should().Be("2");
	}

	[Fact]
	public void BuildLinks_IConnectWithMatchingDollarTwin_Silent()
	{
		// Settings pin: iconnect (feedback) and directPin (input) both captured to the SAME external.
		// Both halves present, so the check says nothing: no Warning, no Debug.
		var pins = new[]
		{
			FakePin.WithIConnect("Root.Kp", "Plant.Kp"),
			FakePin.WithDirectPin("Root.Kp$", "Plant.Kp"),
		};

		var sink = new CapturingSink();
		var logger = sink.ToLogger();

		LinkCollector.BuildLinks(pins, logger);

		sink.Events.Should().NotContain(e =>
			e.MessageTemplate.Text.StartsWith("Iconnect ", StringComparison.Ordinal));
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
		var logger = sink.ToLogger();

		LinkCollector.BuildLinks(pins, logger);

		var warning = sink.Events.Single(e => e.Level == LogEventLevel.Warning);
		warning.MessageTemplate.Text.Should().Be(
			"Iconnect {LocalPin} <-> {ExternalPin} captured without an input link on '{Sibling}'");
		CapturingSink.Property(warning, "LocalPin").Should().Be("Root.Setpoint");
		CapturingSink.Property(warning, "ExternalPin").Should().Be("Plant.TemperatureSP");
		CapturingSink.Property(warning, "Sibling").Should().Be("Root.Setpoint$");
	}

	[Fact]
	public void BuildLinks_IConnectWithDollarTwinToDifferentExternal_LogsDebugNotWarning()
	{
		// Feedback-only pin: the $ sibling is captured but routes to a different external. The input
		// half is present, so this stays at Debug.
		var pins = new[]
		{
			FakePin.WithIConnect("Root.Setpoint", "Plant.TemperatureSP"),
			FakePin.WithDirectPin("Root.Setpoint$", "Plant.Setpoints.Setpoint"),
		};

		var sink = new CapturingSink();
		var logger = sink.ToLogger();

		LinkCollector.BuildLinks(pins, logger);

		sink.Events.Should().NotContain(e => e.Level == LogEventLevel.Warning);

		var debug = sink.Events.Single(e =>
			e.MessageTemplate.Text.StartsWith("Iconnect ", StringComparison.Ordinal));
		debug.Level.Should().Be(LogEventLevel.Debug);
		debug.MessageTemplate.Text.Should().Be(
			"Iconnect {LocalPin} <-> {ExternalPin}: input sibling '{Sibling}' captured to '{SiblingExternal}'");
		CapturingSink.Property(debug, "LocalPin").Should().Be("Root.Setpoint");
		CapturingSink.Property(debug, "ExternalPin").Should().Be("Plant.TemperatureSP");
		CapturingSink.Property(debug, "Sibling").Should().Be("Root.Setpoint$");
		CapturingSink.Property(debug, "SiblingExternal").Should().Be("Plant.Setpoints.Setpoint");
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
