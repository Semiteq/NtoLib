using System;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Test.MbeTable.Core.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Core.Integration.Properties;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "PropertyParsing")]
public sealed class CorePropertyParsingTests
{
    private const string CloseActionName = "Закрыть";
    private const string TimeStringInput = "00:00:05";
    private const int ExpectedSecondsFromTimeString = 5;
    private const short ExpectedDefaultTargetId = 1;
    private const string InvalidTimeString = "25:00:00";

    [Fact]
    public void TimeString_ParsesToSeconds()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        var d = new RecipeTestDriver(facade);
        d.AddWait(0);

        var r = facade.UpdateProperty(0, MandatoryColumns.StepDuration, TimeStringInput);

        r.IsSuccess.Should().BeTrue();

        var snap = facade.CurrentSnapshot;
        snap.TotalDuration.Should().Be(TimeSpan.FromSeconds(ExpectedSecondsFromTimeString));
    }

    [Fact]
    public void EnumTarget_DefaultAssigned()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        var repo = services.GetRequiredService<IActionRepository>();
        var actionId = ActionNameHelper.GetActionIdOrThrow(repo, CloseActionName);

        var d = new RecipeTestDriver(facade);
        d.AddDefaultStep(0).ReplaceAction(0, actionId);

        var step = facade.CurrentSnapshot.Recipe.Steps[0];
        var targetKey = new ColumnIdentifier("target");

        step.Properties[targetKey].GetValue<short>().Value.Should().Be(ExpectedDefaultTargetId);
    }

    [Fact]
    public void InvalidTimeComponent_Fails()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        var d = new RecipeTestDriver(facade);
        d.AddWait(0);

        var r = facade.UpdateProperty(0, MandatoryColumns.StepDuration, InvalidTimeString);

        r.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void NonNumericEnum_Fails()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        var repo = services.GetRequiredService<IActionRepository>();
        var actionId = ActionNameHelper.GetActionIdOrThrow(repo, CloseActionName);

        var d = new RecipeTestDriver(facade);
        d.AddDefaultStep(0).ReplaceAction(0, actionId);

        var targetKey = new ColumnIdentifier("target");
        var r = facade.UpdateProperty(0, targetKey, "abc");

        r.IsFailed.Should().BeTrue();
    }
}