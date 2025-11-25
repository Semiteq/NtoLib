using FluentAssertions;

using NtoLib.Test.MbeTable.Core.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Core.Integration.Timings;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "Timing")]
public sealed class CoreTimingTests
{
	private const int DefaultWaitDurationSeconds = 10;
	private const int WaitStepCount = 3;

	[Fact]
	public void LinearAccumulation_WaitSteps()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).AddWait(1).AddWait(2);

		var snap = facade.CurrentSnapshot;

		snap.TotalDuration.Should().Be(TimeSpan.FromSeconds(DefaultWaitDurationSeconds * WaitStepCount));
		snap.StepStartTimes[2].Should().Be(TimeSpan.FromSeconds(DefaultWaitDurationSeconds * 2));
	}

	[Fact]
	public void NegativeDuration_TreatedAsAbsolute()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		const int NegativeDuration = -5;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, NegativeDuration);

		var snap = facade.CurrentSnapshot;

		snap.TotalDuration.Should().Be(TimeSpan.FromSeconds(Math.Abs(NegativeDuration)));
		snap.IsValid.Should().BeTrue();
	}

	[Fact]
	public void NestedLoops_ComposeTotalDuration()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		const int OuterIterations = 2;
		const int InnerIterations = 2;
		const int BodyDuration = 5;
		const int ExpectedTotal = OuterIterations * InnerIterations * BodyDuration;
		const int ExpectedEnclosingCount = 2;

		var d = new RecipeTestDriver(facade);
		d.AddFor(0, OuterIterations);
		d.AddFor(1, InnerIterations);
		d.AddWait(2).SetDuration(2, BodyDuration);
		d.AddEndFor(3);
		d.AddEndFor(4);

		var snap = facade.CurrentSnapshot;

		snap.TotalDuration.Should().Be(TimeSpan.FromSeconds(ExpectedTotal));

		var enclosing = snap.LoopTree.EnclosingLoopsForStep[2];
		enclosing.Count.Should().Be(ExpectedEnclosingCount);
	}
}
