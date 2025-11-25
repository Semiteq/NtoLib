using FluentAssertions;

using NtoLib.Test.MbeTable.Config.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Config.Integration.ConfigurationLoaderTests;

[Trait("Category", "Integration")]
[Trait("Component", "ConfigLoader")]
public sealed class HappyPathTests
{
	[Fact]
	public void LoadValidBaseline_Succeeds()
	{
		var config = TestHelper.LoadValidCase("Baseline");

		config.PropertyDefinitions.Should().ContainKey("float");
		config.PropertyDefinitions.Should().ContainKey("time");
		config.PropertyDefinitions.Should().ContainKey("string");
		config.PropertyDefinitions.Should().ContainKey("enum");

		config.Columns.Select(c => c.Key.Value).Should().Contain(new[]
		{
			"action", "target", "initial_value", "task", "speed", "step_duration", "step_start_time", "comment"
		});

		config.PinGroupData.Select(g => g.GroupName).Should().Contain(new[] { "Valve", "TempSensor" });

		config.Actions.Should().ContainKey(10);
		config.Actions.Should().ContainKey(1100);
	}

	[Fact]
	public void LoadValidWithNoise_IgnoresUnknownFields()
	{
		var config = TestHelper.LoadValidCase("WithNoiseFields");

		config.Actions.Should().ContainKey(10);
	}
}
