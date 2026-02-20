using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using NtoLib.TrendPensManager.Entities;
using NtoLib.TrendPensManager.Services;

using Tests.TrendPensManager.Helpers;

using Xunit;

namespace Tests.TrendPensManager.Unit;

[Trait("Category", "Unit")]
[Trait("Component", "TrendPensManager")]
public sealed class PenSequenceIntegrationTests
{
	private static string GetTestDataRoot()
	{
		var dir = AppContext.BaseDirectory;
		for (var i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
		{
			var probe = Path.Combine(dir, "TrendPensManager", "TestData");
			if (Directory.Exists(probe))
			{
				return probe;
			}

			dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
		}

		throw new DirectoryNotFoundException("TrendPensManager/TestData root not found");
	}

	[Fact]
	public void BuildPlan_SingleHeaterChannel_UsesConfigNameSuffix()
	{
		var root = GetTestDataRoot();
		var trendDumpPath = Path.Combine(root, "SingleHeaterChannel", "Trend.json");
		var configDumpPath = Path.Combine(root, "SingleHeaterChannel", "Config.json");

		var (channels, traversalWarnings) = TrendPensTestHelper.LoadChannelsFromJson(trendDumpPath);
		var configResult = TrendPensTestHelper.LoadConfigFromJson(configDumpPath);
		configResult.IsSuccess.Should().BeTrue();

		channels.Should().ContainSingle();
		var channel = channels.Single();
		channel.ServiceType.Should().Be(ServiceType.Heaters);
		channel.Used.Should().BeTrue();

		var sequenceBuilder = new PenSequenceBuilder(NullLoggerFactory.Instance);
		var sequenceResult = sequenceBuilder.BuildSequence(
			channels,
			configResult.Value,
			channel.ServiceName.Split('.')[0] + ".Графики");
		sequenceResult.IsSuccess.Should().BeTrue();

		var sequence = sequenceResult.Value.Sequence;
		sequence.Should().HaveCount(2);

		var expectedConfigName = configResult.Value[ServiceType.Heaters][channel.ChannelNumber - 1];
		expectedConfigName.Should().Be("Source1");
		sequence.All(p => p.PenDisplayName.EndsWith(" " + expectedConfigName, StringComparison.Ordinal)).Should()
			.BeTrue();
	}
}
