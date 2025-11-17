using System;

using FluentAssertions;

using NtoLib.Test.MbeTable.Core.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Core.Integration.Baseline;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "Baseline")]
public sealed class CoreBaselineStateTests
{
    [Fact]
    public void EmptyRecipe_IsInvalid_AndHasEmptyReason()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        var snap = facade.CurrentSnapshot;

        snap.StepCount.Should().Be(0);
        snap.IsValid.Should().BeFalse();
        snap.Flags.EmptyRecipe.Should().BeTrue();
        snap.TotalDuration.Should().Be(TimeSpan.Zero);
        snap.Reasons.Should().NotBeEmpty();
    }
}