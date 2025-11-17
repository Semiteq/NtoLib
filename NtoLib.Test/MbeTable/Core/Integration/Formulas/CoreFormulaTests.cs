using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Test.MbeTable.Core.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Core.Integration.Formulas;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "Formulas")]
public sealed class CoreFormulaTests
{
    private const string TemperatureRampActionName = "t°C плавно";
    private const float DefaultTemperatureRampSpeed = 10f;
    private const float DefaultTemperatureRampDuration = 600f;
    private const float CustomSpeed = 20f;
    private const float ExpectedDurationAfterSpeedChange = 300f;

    [Fact]
    public void TemperatureRamp_SpeedChange_RecalculatesDuration()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        var repo = services.GetRequiredService<IActionRepository>();
        var actionId = ActionNameHelper.GetActionIdOrThrow(repo, TemperatureRampActionName);

        var d = new RecipeTestDriver(facade);
        d.AddDefaultStep(0).ReplaceAction(0, actionId);

        var before = facade.CurrentSnapshot.Recipe.Steps[0]
            .Properties[MandatoryColumns.StepDuration]
            .GetValue<float>().Value;
        before.Should().Be(DefaultTemperatureRampDuration);

        d.SetSpeed(0, CustomSpeed);

        var after = facade.CurrentSnapshot.Recipe.Steps[0]
            .Properties[MandatoryColumns.StepDuration]
            .GetValue<float>().Value;
        after.Should().Be(ExpectedDurationAfterSpeedChange);
    }

    [Fact]
    public void Idempotent_UpdateSameSpeed_NoChange()
    {
        var (services, facade) = CoreTestHelper.BuildCore();
        using var _ = services as IDisposable;

        var repo = services.GetRequiredService<IActionRepository>();
        var actionId = ActionNameHelper.GetActionIdOrThrow(repo, TemperatureRampActionName);

        var d = new RecipeTestDriver(facade);
        d.AddDefaultStep(0).ReplaceAction(0, actionId);
        d.SetSpeed(0, DefaultTemperatureRampSpeed);

        var before = facade.CurrentSnapshot.Recipe.Steps[0]
            .Properties[MandatoryColumns.StepDuration]
            .GetValue<float>().Value;

        d.SetSpeed(0, DefaultTemperatureRampSpeed);

        var after = facade.CurrentSnapshot.Recipe.Steps[0]
            .Properties[MandatoryColumns.StepDuration]
            .GetValue<float>().Value;

        after.Should().Be(before);
    }
}