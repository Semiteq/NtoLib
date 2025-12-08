using FluentAssertions;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

using Tests.MbeTable.Core.Helpers;

using Xunit;

namespace Tests.MbeTable.Core.Integration.Snapshot;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "Snapshot")]
public sealed class CoreSnapshotStateTests
{
	private const int FirstStepDuration = 5;
	private const int DefaultWaitDuration = 10;
	private const int SecondStepDuration = 5;
	private const int LoopIterations = 2;

	[Fact]
	public void LastValidSnapshot_PreservedAcrossInvalidTransition()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, FirstStepDuration);

		var firstValid = facade.LastValidSnapshot;
		firstValid.Should().NotBeNull();

		d.AddDefaultStep(1).ReplaceAction(1, (short)ServiceActions.EndForLoop);

		var current = facade.CurrentSnapshot;
		current.IsValid.Should().BeFalse();

		var lastValid = facade.LastValidSnapshot;
		lastValid.Should().NotBeNull();
		lastValid!.TotalDuration.Should().Be(TimeSpan.FromSeconds(FirstStepDuration + DefaultWaitDuration));
	}

	[Fact]
	public void LastValidSnapshot_UpdatesAfterFix()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddFor(0, LoopIterations);
		d.AddWait(1).SetDuration(1, SecondStepDuration);

		facade.CurrentSnapshot.IsValid.Should().BeFalse();

		d.AddEndFor(2);

		var fixedSnap = facade.CurrentSnapshot;
		fixedSnap.IsValid.Should().BeTrue();

		facade.LastValidSnapshot!.TotalDuration.Should().Be(fixedSnap.TotalDuration);
	}
}
