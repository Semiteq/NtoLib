using FluentAssertions;

using NtoLib.Recipes.MbeTable.ModuleConfig.Common;

namespace Tests.MbeTable.Config.Helpers;

public static class ErrorAssertions
{
	public static ConfigError ShouldContainError(this ConfigException ex, string section, string contextContains)
	{
		var error = ex.Errors.FirstOrDefault(e =>
			string.Equals(e.Section, section, StringComparison.OrdinalIgnoreCase) &&
			(e.Context?.IndexOf(contextContains, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);

		error.Should().NotBeNull($"Expected error in section '{section}' with context containing '{contextContains}'.");
		return error!;
	}

	public static void ShouldHaveSection(this ConfigError error, string section)
	{
		error.Section.Should().Be(section);
	}

	public static void ShouldHaveContextContaining(this ConfigError error, string expectedPart)
	{
		error.Context.Should().Contain(expectedPart);
	}

	public static void ShouldHaveMetadata(this ConfigError error, string key)
	{
		error.Metadata.Should().ContainKey(key);
	}
}
