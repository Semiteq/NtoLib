using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;
using NtoLib.Test.MbeTable.Core.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Core.Integration.Timer;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "TimerLoop")]
public sealed class CoreTimerLoopTests
{
    private const int BodyDuration = 5;
    private const int Iterations = 3;

    [Fact]
    public void SimpleLoop_FirstIteration_NoOffset()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        var d = new RecipeTestDriver(facade);
        d.AddFor(0, Iterations);
        d.AddWait(1).SetDuration(1, BodyDuration);
        d.AddEndFor(2);

        var timer = services.GetRequiredService<ITimerService>();
        TimeSpan capturedStepLeft = TimeSpan.Zero;
        TimeSpan capturedTotalLeft = TimeSpan.Zero;

        timer.TimesUpdated += (stepLeft, totalLeft) =>
        {
            capturedStepLeft = stepLeft;
            capturedTotalLeft = totalLeft;
        };

        var analysis = facade.CurrentSnapshot;
        var runtime = RuntimeSnapshotBuilder.CreateActive(
            stepIndex: 1,
            stepElapsed: 0f,
            for1: 0);

        timer.UpdateRuntime(runtime, analysis);

        capturedStepLeft.Should().Be(TimeSpan.FromSeconds(BodyDuration));
        capturedTotalLeft.Should().Be(TimeSpan.FromSeconds(BodyDuration * Iterations));
    }

    [Fact]
    public void SimpleLoop_SecondIteration_OffsetApplied()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        var d = new RecipeTestDriver(facade);
        d.AddFor(0, Iterations);
        d.AddWait(1).SetDuration(1, BodyDuration);
        d.AddEndFor(2);

        var timer = services.GetRequiredService<ITimerService>();
        TimeSpan capturedStepLeft = TimeSpan.Zero;
        TimeSpan capturedTotalLeft = TimeSpan.Zero;

        timer.TimesUpdated += (stepLeft, totalLeft) =>
        {
            capturedStepLeft = stepLeft;
            capturedTotalLeft = totalLeft;
        };

        var analysis = facade.CurrentSnapshot;
        var runtime = RuntimeSnapshotBuilder.CreateActive(
            stepIndex: 1,
            stepElapsed: 0f,
            for1: 1);

        timer.UpdateRuntime(runtime, analysis);

        capturedStepLeft.Should().Be(TimeSpan.FromSeconds(BodyDuration));
        capturedTotalLeft.Should().Be(TimeSpan.FromSeconds(BodyDuration * 2));
    }

    [Fact]
    public void NestedLoops_OuterIterationComplete_CorrectOffset()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        const int outerIterations = 2;
        const int innerIterations = 2;
        const int expectedOffset = outerIterations * innerIterations * BodyDuration;

        var d = new RecipeTestDriver(facade);
        d.AddFor(0, outerIterations);
        d.AddFor(1, innerIterations);
        d.AddWait(2).SetDuration(2, BodyDuration);
        d.AddEndFor(3);
        d.AddEndFor(4);

        var timer = services.GetRequiredService<ITimerService>();
        TimeSpan capturedStepLeft = TimeSpan.Zero;
        TimeSpan capturedTotalLeft = TimeSpan.Zero;

        timer.TimesUpdated += (stepLeft, totalLeft) =>
        {
            capturedStepLeft = stepLeft;
            capturedTotalLeft = totalLeft;
        };

        var analysis = facade.CurrentSnapshot;
        var runtime = RuntimeSnapshotBuilder.CreateActive(
            stepIndex: 2,
            stepElapsed: 0f,
            for1: 1,
            for2: 0);

        timer.UpdateRuntime(runtime, analysis);

        capturedTotalLeft.Should().Be(TimeSpan.FromSeconds(expectedOffset / 2));
    }

    [Fact]
    public void LoopCountClamp_AtMaxIterations_UsesMaxMinusOne()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        var d = new RecipeTestDriver(facade);
        d.AddFor(0, Iterations);
        d.AddWait(1).SetDuration(1, BodyDuration);
        d.AddEndFor(2);

        var timer = services.GetRequiredService<ITimerService>();
        TimeSpan capturedTotalLeft = TimeSpan.Zero;

        timer.TimesUpdated += (stepLeft, totalLeft) => { capturedTotalLeft = totalLeft; };

        var analysis = facade.CurrentSnapshot;
        var runtime = RuntimeSnapshotBuilder.CreateActive(
            stepIndex: 1,
            stepElapsed: 0f,
            for1: Iterations);

        timer.UpdateRuntime(runtime, analysis);

        const int clampedIterations = Iterations - 1;
        const int expectedRemaining = (Iterations - clampedIterations) * BodyDuration;
        capturedTotalLeft.Should().Be(TimeSpan.FromSeconds(expectedRemaining));
    }

    [Fact]
    public void LoopIntegrityBroken_IgnoresOffset()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        var d = new RecipeTestDriver(facade);
        d.AddFor(0, Iterations);
        d.AddWait(1).SetDuration(1, BodyDuration);

        var timer = services.GetRequiredService<ITimerService>();
        TimeSpan capturedTotalLeft = TimeSpan.Zero;

        timer.TimesUpdated += (stepLeft, totalLeft) => { capturedTotalLeft = totalLeft; };

        var analysis = facade.CurrentSnapshot;
        var runtime = RuntimeSnapshotBuilder.CreateActive(
            stepIndex: 1,
            stepElapsed: 0f,
            for1: 1);

        timer.UpdateRuntime(runtime, analysis);

        capturedTotalLeft.Should().Be(TimeSpan.FromSeconds(BodyDuration));
    }
}