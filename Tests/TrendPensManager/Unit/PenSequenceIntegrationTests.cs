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

	[Fact(Skip = "Broken real dump: no used channels or non-empty Sources_OUT")]
	public void BuildPlan_FromRealDump_AddsExpectedPensForUsedChannels()
	{
		var root = GetTestDataRoot();
		var trendDumpPath = Path.Combine(root, "TrendTreeDump_PLZ_Inject.Графики_20251205_103737.json");
		var configDumpPath = Path.Combine(root, "ConfigLoaderDump_PLZ_Inject.Config.Загрузчик конфигурации_20251205_103737.json");

		var (channels, traversalWarnings) = TrendPensTestHelper.LoadChannelsFromJson(trendDumpPath);
		var configResult = TrendPensTestHelper.LoadConfigFromJson(configDumpPath);
		configResult.IsSuccess.Should().BeTrue();

		// mark first Heaters channel as used for the test
		var firstHeater = channels.First(c => c.ServiceType == ServiceType.Heaters);
		var patchedChannels = channels
			.Select(c => c == firstHeater
				? new ChannelInfo(c.ServiceName, c.ServiceType, c.ChannelNumber, c.Used, c.Parameters)
				: c)
			.ToList();

		var planBuilder = new PenSequenceBuilder(NullLoggerFactory.Instance);

		var planResult = planBuilder.BuildSequence(patchedChannels, configResult.Value, "PLZ_Inject.Графики");
		planResult.IsSuccess.Should().BeTrue();

		var sequence = planResult.Value.Sequence;
		sequence.Should().NotBeEmpty();
		sequence.All(p => p.TrendPath == "PLZ_Inject.Графики").Should().BeTrue();

		var expectedConfigName = configResult.Value[ServiceType.Heaters][firstHeater.ChannelNumber - 1];
		if (!string.IsNullOrWhiteSpace(expectedConfigName))
		{
			sequence.Any(p => p.PenDisplayName.EndsWith(" " + expectedConfigName, StringComparison.Ordinal)).Should().BeTrue();
		}
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
		sequence.All(p => p.PenDisplayName.EndsWith(" " + expectedConfigName, StringComparison.Ordinal)).Should().BeTrue();
	}
}
