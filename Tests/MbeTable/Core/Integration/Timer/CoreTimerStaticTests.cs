using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;

using Tests.MbeTable.Core.Helpers;

using Xunit;

namespace Tests.MbeTable.Core.Integration.Timer;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "TimerStatic")]
public sealed class CoreTimerStaticTests
{
	[Fact]
	public void EmptyRecipe_Inactive_ReturnsZeros()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var timer = services.GetRequiredService<ITimerService>();
		TimeSpan capturedStepLeft = TimeSpan.Zero;
		TimeSpan capturedTotalLeft = TimeSpan.Zero;

		timer.TimesUpdated += (stepLeft, totalLeft) =>
		{
			capturedStepLeft = stepLeft;
			capturedTotalLeft = totalLeft;
		};

		var analysis = facade.CurrentSnapshot;
		var runtime = RuntimeSnapshotBuilder.CreateInactive();

		timer.UpdateRuntime(runtime, analysis);

		capturedStepLeft.Should().Be(TimeSpan.Zero);
		capturedTotalLeft.Should().Be(TimeSpan.Zero);
	}

	[Fact]
	public void RecipeWithSteps_Inactive_ReturnsZeroStepLeftAndTotalDuration()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, 10f);
		d.AddWait(1).SetDuration(1, 5f);

		var timer = services.GetRequiredService<ITimerService>();
		TimeSpan capturedStepLeft = TimeSpan.Zero;
		TimeSpan capturedTotalLeft = TimeSpan.Zero;

		timer.TimesUpdated += (stepLeft, totalLeft) =>
		{
			capturedStepLeft = stepLeft;
			capturedTotalLeft = totalLeft;
		};

		var analysis = facade.CurrentSnapshot;
		var runtime = RuntimeSnapshotBuilder.CreateInactive();

		timer.UpdateRuntime(runtime, analysis);

		capturedStepLeft.Should().Be(TimeSpan.Zero);
		capturedTotalLeft.Should().Be(TimeSpan.FromSeconds(15));
	}

	[Fact]
	public void RecipeActiveTransition_FalseToTrue_ResetsState()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, 10f);

		var timer = services.GetRequiredService<ITimerService>();
		var analysis = facade.CurrentSnapshot;

		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateInactive(), analysis);

		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(0, 0f), analysis);

		TimeSpan capturedStepLeft = TimeSpan.Zero;
		TimeSpan capturedTotalLeft = TimeSpan.Zero;

		timer.TimesUpdated += (stepLeft, totalLeft) =>
		{
			capturedStepLeft = stepLeft;
			capturedTotalLeft = totalLeft;
		};

		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(0, 0f), analysis);

		capturedStepLeft.Should().Be(TimeSpan.FromSeconds(10));
		capturedTotalLeft.Should().Be(TimeSpan.FromSeconds(10));
	}
}
