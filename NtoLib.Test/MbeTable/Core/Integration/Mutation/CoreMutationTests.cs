using FluentAssertions;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Test.MbeTable.Core.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Core.Integration.Mutation;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "Mutation")]
public sealed class CoreMutationTests
{
	private const int DefaultWaitDurationSeconds = 10;

	[Fact]
	public void AddStep_DefaultAction_AssignsProperties()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		facade.AddStep(0).IsSuccess.Should().BeTrue();

		var snap = facade.CurrentSnapshot;
		snap.StepCount.Should().Be(1);
		snap.IsValid.Should().BeTrue();

		var step = snap.Recipe.Steps[0];
		step.Properties.Should().ContainKey(MandatoryColumns.Action);
	}

	[Fact]
	public void InsertStep_ShiftsStartTimes()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).AddWait(1);

		var beforeSecond = facade.CurrentSnapshot.StepStartTimes[1];
		beforeSecond.Should().Be(TimeSpan.FromSeconds(DefaultWaitDurationSeconds));

		d.AddWait(1);

		var snap = facade.CurrentSnapshot;
		snap.StepCount.Should().Be(3);
		snap.StepStartTimes[2].Should().Be(TimeSpan.FromSeconds(DefaultWaitDurationSeconds * 2));
	}

	[Fact]
	public void RemoveStep_RecalculatesStartTimes()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).AddWait(1).AddWait(2);

		var snap = facade.CurrentSnapshot;
		snap.StepStartTimes[2].Should().Be(TimeSpan.FromSeconds(DefaultWaitDurationSeconds * 2));

		facade.RemoveStep(1).IsSuccess.Should().BeTrue();

		var after = facade.CurrentSnapshot;
		after.StepCount.Should().Be(2);
		after.StepStartTimes[1].Should().Be(TimeSpan.FromSeconds(DefaultWaitDurationSeconds));
	}

	[Fact]
	public void ReplaceAction_LongLastingToImmediate_RemovesDurationEffect()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		const int CustomDuration = 12;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, CustomDuration);

		var before = facade.CurrentSnapshot.TotalDuration;
		before.Should().Be(TimeSpan.FromSeconds(CustomDuration));

		d.ReplaceAction(0, (short)ServiceActions.ForLoop);

		var after = facade.CurrentSnapshot;
		after.TotalDuration.Should().Be(TimeSpan.Zero);
	}

	[Fact]
	public void UpdateProperty_InvalidIndex_Fails()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		const int InvalidIndex = 5;

		var result = facade.UpdateProperty(InvalidIndex, MandatoryColumns.Task, 10f);

		result.IsFailed.Should().BeTrue();
	}
}
