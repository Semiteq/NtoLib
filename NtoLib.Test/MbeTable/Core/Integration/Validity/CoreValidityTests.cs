using System;
using System.Linq;

using FluentAssertions;

using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Test.MbeTable.Core.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Core.Integration.Validity;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "Validity")]
public sealed class CoreValidityTests
{
    private const int LoopIterations = 2;
    private const int StepDuration = 5;
    private const int MaxAllowedNestingDepth = 3;

    [Fact]
    public void ValidityFormula_TrueOnlyWhenNoFlagsAndNoErrors()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        var d = new RecipeTestDriver(facade);
        d.AddWait(0);

        var snap = facade.CurrentSnapshot;

        snap.IsValid.Should().BeTrue();
        snap.Flags.EmptyRecipe.Should().BeFalse();
        snap.Reasons.Any(r => r is BilingualError).Should().BeFalse();
    }

    [Fact]
    public void EmptyRecipe_Invalid()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        var snap = facade.CurrentSnapshot;

        snap.IsValid.Should().BeFalse();
        snap.Flags.EmptyRecipe.Should().BeTrue();
    }

    [Fact]
    public void LoopIntegrity_Invalidates()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        var d = new RecipeTestDriver(facade);
        d.AddFor(0, LoopIterations);
        d.AddWait(1).SetDuration(1, StepDuration);

        var snap = facade.CurrentSnapshot;

        snap.IsValid.Should().BeFalse();
        snap.Flags.LoopIntegrityCompromised.Should().BeTrue();
    }

    [Fact]
    public void MaxDepth_Invalidates()
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