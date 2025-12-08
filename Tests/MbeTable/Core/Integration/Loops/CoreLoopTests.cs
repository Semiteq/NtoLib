using FluentAssertions;

using Tests.MbeTable.Core.Helpers;

using Xunit;

namespace Tests.MbeTable.Core.Integration.Loops;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "Loops")]
public sealed class CoreLoopTests
{
	private const int SingleIterationDuration = 4;
	private const int IterationCount = 3;
	private const int MaxAllowedNestingDepth = 3;

	[Fact]
	public void ClosedLoop_ComputesIterationTiming()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddFor(0, IterationCount);
		d.AddWait(1).SetDuration(1, SingleIterationDuration);
		d.AddEndFor(2);

		var snap = facade.CurrentSnapshot;

		snap.IsValid.Should().BeTrue();
		snap.TotalDuration.Should().Be(TimeSpan.FromSeconds(SingleIterationDuration * IterationCount));
		snap.LoopTree.ByStartIndex.ContainsKey(0).Should().BeTrue();

		var loop = snap.LoopTree.ByStartIndex[0];
		loop.EffectiveIterationCount.Should().Be(IterationCount);
		loop.SingleIterationDuration.Should().Be(TimeSpan.FromSeconds(SingleIterationDuration));
	}

	[Fact]
	public void UnclosedLoop_InvalidatesRecipe()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddFor(0, 2);
		d.AddWait(1).SetDuration(1, 5f);

		var snap = facade.CurrentSnapshot;

		snap.IsValid.Should().BeFalse();
		snap.Flags.LoopIntegrityCompromised.Should().BeTrue();
	}

	[Fact]
	public void MaxDepthExceeded_InvalidAndHasWarning()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);

		for (int i = 0; i <= MaxAllowedNestingDepth; i++)
		{
			d.AddFor(i, 1);
		}

		d.AddWait(MaxAllowedNestingDepth + 1).SetDuration(MaxAllowedNestingDepth + 1, 2f);

		for (int i = MaxAllowedNestingDepth + 2; i <= (MaxAllowedNestingDepth * 2) + 2; i++)
		{
			d.AddEndFor(i);
		}

		var snap = facade.CurrentSnapshot;

		snap.IsValid.Should().BeFalse();
		snap.Flags.MaxDepthExceeded.Should().BeTrue();
	}
}
