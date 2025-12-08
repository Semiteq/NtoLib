using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;

using Tests.MbeTable.Core.Helpers;

using Xunit;

namespace Tests.MbeTable.Core.Integration.Timer;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "TimerEdgeCases")]
public sealed class CoreTimerEdgeCasesTests
{
	private const int StepDuration = 10;

	[Fact]
	public void InvalidStepIndex_Negative_ReturnsZeros()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, StepDuration);

		var timer = services.GetRequiredService<ITimerService>();
		TimeSpan capturedStepLeft = TimeSpan.Zero;
		TimeSpan capturedTotalLeft = TimeSpan.Zero;

		timer.TimesUpdated += (stepLeft, totalLeft) =>
		{
			capturedStepLeft = stepLeft;
			capturedTotalLeft = totalLeft;
		};

		var analysis = facade.CurrentSnapshot;
		var runtime = RuntimeSnapshotBuilder.CreateActive(stepIndex: -1, stepElapsed: 0f);

		timer.UpdateRuntime(runtime, analysis);

		capturedStepLeft.Should().Be(TimeSpan.Zero);
		capturedTotalLeft.Should().Be(TimeSpan.Zero);
	}

	[Fact]
	public void InvalidStepIndex_BeyondCount_ReturnsZeros()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, StepDuration);

		var timer = services.GetRequiredService<ITimerService>();
		TimeSpan capturedStepLeft = TimeSpan.Zero;
		TimeSpan capturedTotalLeft = TimeSpan.Zero;

		timer.TimesUpdated += (stepLeft, totalLeft) =>
		{
			capturedStepLeft = stepLeft;
			capturedTotalLeft = totalLeft;
		};

		var analysis = facade.CurrentSnapshot;
		var runtime = RuntimeSnapshotBuilder.CreateActive(stepIndex: 100, stepElapsed: 0f);

		timer.UpdateRuntime(runtime, analysis);

		capturedStepLeft.Should().Be(TimeSpan.Zero);
		capturedTotalLeft.Should().Be(TimeSpan.Zero);
	}

	[Fact]
	public void NegativeForLoopCount_ClampsToZero()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		const int Iterations = 3;
		const int BodyDuration = 5;

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
			for1: -5);

		timer.UpdateRuntime(runtime, analysis);

		capturedTotalLeft.Should().Be(TimeSpan.FromSeconds(Iterations * BodyDuration));
	}

	[Fact]
	public void Regression_SameStep_ClampsToLastValue()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, StepDuration);

		var timer = services.GetRequiredService<ITimerService>();
		var analysis = facade.CurrentSnapshot;

		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(0, 5f), analysis);

		TimeSpan capturedTotalLeft = TimeSpan.Zero;

		timer.TimesUpdated += (stepLeft, totalLeft) => { capturedTotalLeft = totalLeft; };

		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(0, 3f), analysis);

		capturedTotalLeft.Should().Be(TimeSpan.FromSeconds(5));
	}

	[Fact]
	public void Regression_DifferentStep_Allowed()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, StepDuration);
		d.AddWait(1).SetDuration(1, 5f);

		var timer = services.GetRequiredService<ITimerService>();
		var analysis = facade.CurrentSnapshot;

		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(0, 9f), analysis);

		TimeSpan capturedTotalLeft = TimeSpan.Zero;

		timer.TimesUpdated += (stepLeft, totalLeft) => { capturedTotalLeft = totalLeft; };

		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(1, 0f), analysis);

		capturedTotalLeft.Should().Be(TimeSpan.FromSeconds(5));
	}

	[Fact]
	public void EmptyRecipe_Active_ReturnsZeros()
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
		var runtime = RuntimeSnapshotBuilder.CreateActive(0, 0f);

		timer.UpdateRuntime(runtime, analysis);

		capturedStepLeft.Should().Be(TimeSpan.Zero);
		capturedTotalLeft.Should().Be(TimeSpan.Zero);
	}
}
