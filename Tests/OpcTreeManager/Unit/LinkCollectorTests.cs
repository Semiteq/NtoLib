using FluentAssertions;

using MasterSCADALib;

using NtoLib.OpcTreeManager.TreeOperations;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

public sealed class LinkCollectorTests
{
	[Fact]
	public void BuildLinks_IConnectTwin_EmitsSingleRowFromNonDollarPin()
	{
		var pins = new[]
		{
			FakePin.WithIConnect("Root.Setpoint", "Consumer.Value"),
			FakePin.WithDirectPin("Root.Setpoint$", "Consumer.Value"),
		};

		var result = LinkCollector.BuildLinks(pins, log: null);

		result.Should().HaveCount(1);
		result[0].LocalPinPath.Should().Be("Root.Setpoint");
		result[0].ExternalPinPath.Should().Be("Consumer.Value");
		result[0].LinkType.Should().Be("iconnect");
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
