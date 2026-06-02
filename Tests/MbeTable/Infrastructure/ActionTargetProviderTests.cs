using System;
using System.Collections.Generic;

using FluentAssertions;

using NtoLib.Recipes.MbeTable;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.ActionTarget;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.Errors;

using Xunit;

namespace Tests.MbeTable.Infrastructure;

public sealed class ActionTargetProviderTests
{
	private const string GroupName = "Shutter";

	[Fact]
	public void GetFilteredGroupTargets_WithKnownGroup_ReturnsNonEmptyTargets()
	{
		var reader = new FakePinGroupReader(
			new[] { GroupName },
			new Dictionary<string, IReadOnlyDictionary<int, string>>
			{
				[GroupName] = new Dictionary<int, string> { { 1, "Al" }, { 2, "" }, { 3, "Ga" } }
			});
		var provider = new ActionTargetProvider(reader);

		var result = provider.GetFilteredGroupTargets(GroupName);

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		result.Value.Should().ContainKey((short)1).WhoseValue.Should().Be("Al");
		result.Value.Should().ContainKey((short)3).WhoseValue.Should().Be("Ga");
	}

	[Fact]
	public void GetFilteredGroupTargets_WithUnknownGroup_ReturnsTargetsNotDefinedError()
	{
		var reader = new FakePinGroupReader(Array.Empty<string>(), new Dictionary<string, IReadOnlyDictionary<int, string>>());
		var provider = new ActionTargetProvider(reader);

		var result = provider.GetFilteredGroupTargets("Unknown");

		result.IsFailed.Should().BeTrue();
		result.HasError<InfrastructureTargetsNotDefinedError>().Should().BeTrue();
	}

	[Fact]
	public void GetFilteredGroupTargets_WithEmptyButPresentGroup_ReturnsEmptySuccess()
	{
		var reader = new FakePinGroupReader(
			new[] { GroupName },
			new Dictionary<string, IReadOnlyDictionary<int, string>>
			{
				[GroupName] = new Dictionary<int, string>()
			});
		var provider = new ActionTargetProvider(reader);

		var result = provider.GetFilteredGroupTargets(GroupName);

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public void GetFilteredGroupTargets_WithNullGroup_Throws()
	{
		var reader = new FakePinGroupReader(Array.Empty<string>(), new Dictionary<string, IReadOnlyDictionary<int, string>>());
		var provider = new ActionTargetProvider(reader);

		var act = () => provider.GetFilteredGroupTargets(null);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void GetMinimalTargetId_WithNullGroup_Throws()
	{
		var reader = new FakePinGroupReader(Array.Empty<string>(), new Dictionary<string, IReadOnlyDictionary<int, string>>());
		var provider = new ActionTargetProvider(reader);

		var act = () => provider.GetMinimalTargetId(null);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void GetMinimalTargetId_ReturnsLowestNonEmptyKey()
	{
		var reader = new FakePinGroupReader(
			new[] { GroupName },
			new Dictionary<string, IReadOnlyDictionary<int, string>>
			{
				[GroupName] = new Dictionary<int, string> { { 5, "Ga" }, { 2, "Al" }, { 3, "" } }
			});
		var provider = new ActionTargetProvider(reader);

		var result = provider.GetMinimalTargetId(GroupName);

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().Be(2);
	}

	[Fact]
	public void GetMinimalTargetId_WhenAllTargetsEmpty_ReturnsFailure()
	{
		var reader = new FakePinGroupReader(
			new[] { GroupName },
			new Dictionary<string, IReadOnlyDictionary<int, string>>
			{
				[GroupName] = new Dictionary<int, string> { { 1, "" }, { 2, "" } }
			});
		var provider = new ActionTargetProvider(reader);

		var result = provider.GetMinimalTargetId(GroupName);

		result.IsFailed.Should().BeTrue();
		result.HasError<InfrastructureTargetGroupEmptyError>().Should().BeTrue();
	}

	[Fact]
	public void GetAllTargetsFilteredSnapshot_ReturnsAllGroupsWithoutEmptyValues()
	{
		var reader = new FakePinGroupReader(
			new[] { "Shutter", "Heater" },
			new Dictionary<string, IReadOnlyDictionary<int, string>>
			{
				["Shutter"] = new Dictionary<int, string> { { 1, "Al" }, { 2, "" } },
				["Heater"] = new Dictionary<int, string> { { 1, "H1" } }
			});
		var provider = new ActionTargetProvider(reader);

		var snapshot = provider.GetAllTargetsFilteredSnapshot();

		snapshot.Should().HaveCount(2);
		snapshot["Shutter"].Should().HaveCount(1);
		snapshot["Heater"].Should().HaveCount(1);
	}

	private sealed class FakePinGroupReader : IPinGroupReader
	{
		private readonly IReadOnlyCollection<string> _groupNames;
		private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> _targets;

		public FakePinGroupReader(
			IReadOnlyCollection<string> groupNames,
			IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> targets)
		{
			_groupNames = groupNames;
			_targets = targets;
		}

		public IReadOnlyCollection<string> GetDefinedGroupNames()
		{
			return _groupNames;
		}

		public IReadOnlyDictionary<int, string> ReadTargets(string groupName)
		{
			return _targets[groupName];
		}
	}
}
