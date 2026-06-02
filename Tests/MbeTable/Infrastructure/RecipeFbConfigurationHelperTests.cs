using System;
using System.Collections.Generic;

using FluentAssertions;

using NtoLib.Recipes.MbeTable;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.PinGroups;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties.Contracts;

using Xunit;

namespace Tests.MbeTable.Infrastructure;

/// <summary>
/// Covers the COM-neutral config/pin logic shared by both recipe FB shells. The same
/// guards previously lived (duplicated) inside each FB partial.
/// </summary>
public sealed class RecipeFbConfigurationHelperTests
{
	private const string GroupName = "Shutter";

	private static AppConfiguration ConfigurationWith(params PinGroupData[] groups)
	{
		return new AppConfiguration(
			new Dictionary<string, IPropertyTypeDefinition>(),
			Array.Empty<ColumnDefinition>(),
			new Dictionary<short, ActionDefinition>(),
			groups);
	}

	[Fact]
	public void GetDefinedGroupNames_ReturnsConfiguredGroupNames()
	{
		var config = ConfigurationWith(
			new PinGroupData(GroupName, 1, 201, 2),
			new PinGroupData("Heater", 2, 301, 1));

		var names = RecipeFbConfigurationHelper.GetDefinedGroupNames(config);

		names.Should().BeEquivalentTo(GroupName, "Heater");
	}

	[Fact]
	public void ReadTargets_WithNullGroupName_ThrowsArgumentNullException()
	{
		var config = ConfigurationWith(new PinGroupData(GroupName, 1, 201, 1));

		var act = () => RecipeFbConfigurationHelper.ReadTargets(config, null!, _ => "x");

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ReadTargets_WithUnknownGroup_ThrowsInvalidOperationException()
	{
		var config = ConfigurationWith(new PinGroupData(GroupName, 1, 201, 1));

		var act = () => RecipeFbConfigurationHelper.ReadTargets(config, "Unknown", _ => "x");

		act.Should().Throw<InvalidOperationException>();
	}

	[Fact]
	public void ReadTargets_SkipsPinsThatCannotBeRead()
	{
		var config = ConfigurationWith(new PinGroupData(GroupName, 1, 201, 3));

		var result = RecipeFbConfigurationHelper.ReadTargets(
			config,
			GroupName,
			pinId => pinId == 202 ? null : $"pin{pinId}");

		// Offsets are 1-based; pin 202 (offset 2) is skipped.
		result.Should().HaveCount(2);
		result.Should().ContainKey(1).WhoseValue.Should().Be("pin201");
		result.Should().ContainKey(3).WhoseValue.Should().Be("pin203");
	}

	[Fact]
	public void EnumerateGroupPins_ProducesSequentialIdsAndOneBasedNames()
	{
		var pins = RecipeFbConfigurationHelper.EnumerateGroupPins(new PinGroupData(GroupName, 1, 201, 2));

		pins.Should().BeEquivalentTo(new[]
		{
			(PinId: 201, PinName: "Shutter1"),
			(PinId: 202, PinName: "Shutter2")
		});
	}
}
