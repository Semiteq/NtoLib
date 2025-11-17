using System;

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

        const int negativeDuration = -5;

        var d = new RecipeTestDriver(facade);
        d.AddWait(0).SetDuration(0, negativeDuration);

        var snap = facade.CurrentSnapshot;

        snap.TotalDuration.Should().Be(TimeSpan.FromSeconds(Math.Abs(negativeDuration)));
        snap.IsValid.Should().BeTrue();
    }

    [Fact]
    public void NestedLoops_ComposeTotalDuration()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        const int outerIterations = 2;
        const int innerIterations = 2;
        const int bodyDuration = 5;
        const int expectedTotal = outerIterations * innerIterations * bodyDuration;
        const int expectedEnclosingCount = 2;

        var d = new RecipeTestDriver(facade);
        d.AddFor(0, outerIterations);
        d.AddFor(1, innerIterations);
        d.AddWait(2).SetDuration(2, bodyDuration);
        d.AddEndFor(3);
        d.AddEndFor(4);

        var snap = facade.CurrentSnapshot;

        snap.TotalDuration.Should().Be(TimeSpan.FromSeconds(expectedTotal));

        var enclosing = snap.LoopTree.EnclosingLoopsForStep[2];
        enclosing.Count.Should().Be(expectedEnclosingCount);
    }
}