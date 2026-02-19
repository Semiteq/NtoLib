using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;

using Tests.MbeTable.Core.Helpers;

using Xunit;

namespace Tests.MbeTable.Core.Integration.Timer;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "TimerLinear")]
public sealed class CoreTimerLinearTests
{
	private const int FirstStepDuration = 10;
	private const int SecondStepDuration = 5;
	private const int TotalDuration = FirstStepDuration + SecondStepDuration;

	[Fact]
	public void FirstStep_StartOfExecution_FullDurationsRemaining()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, FirstStepDuration);
		d.AddWait(1).SetDuration(1, SecondStepDuration);

		var timer = services.GetRequiredService<TimerService>();
		TimeSpan capturedStepLeft = TimeSpan.Zero;
		TimeSpan capturedTotalLeft = TimeSpan.Zero;

		timer.TimesUpdated += (stepLeft, totalLeft) =>
		{
			capturedStepLeft = stepLeft;
			capturedTotalLeft = totalLeft;
		};

		var analysis = facade.CurrentSnapshot;
		var runtime = RuntimeSnapshotBuilder.CreateActive(stepIndex: 0, stepElapsed: 0f);

		timer.UpdateRuntime(runtime, analysis);

		capturedStepLeft.Should().Be(TimeSpan.FromSeconds(FirstStepDuration));
		capturedTotalLeft.Should().Be(TimeSpan.FromSeconds(TotalDuration));
	}

	[Fact]
	public void FirstStep_MidExecution_PartialStepLeft()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, FirstStepDuration);
		d.AddWait(1).SetDuration(1, SecondStepDuration);

		var timer = services.GetRequiredService<TimerService>();
		TimeSpan capturedStepLeft = TimeSpan.Zero;
		TimeSpan capturedTotalLeft = TimeSpan.Zero;

		timer.TimesUpdated += (stepLeft, totalLeft) =>
		{
			capturedStepLeft = stepLeft;
			capturedTotalLeft = totalLeft;
		};

		var analysis = facade.CurrentSnapshot;
		var runtime = RuntimeSnapshotBuilder.CreateActive(stepIndex: 0, stepElapsed: 5f);

		timer.UpdateRuntime(runtime, analysis);

		capturedStepLeft.Should().Be(TimeSpan.FromSeconds(5));
		capturedTotalLeft.Should().Be(TimeSpan.FromSeconds(10));
	}

	[Fact]
	public void SecondStep_Beginning_CorrectTotalLeft()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, FirstStepDuration);
		d.AddWait(1).SetDuration(1, SecondStepDuration);

		var timer = services.GetRequiredService<TimerService>();
		TimeSpan capturedStepLeft = TimeSpan.Zero;
		TimeSpan capturedTotalLeft = TimeSpan.Zero;

		timer.TimesUpdated += (stepLeft, totalLeft) =>
		{
			capturedStepLeft = stepLeft;
			capturedTotalLeft = totalLeft;
		};

		var analysis = facade.CurrentSnapshot;
		var runtime = RuntimeSnapshotBuilder.CreateActive(stepIndex: 1, stepElapsed: 0f);

		timer.UpdateRuntime(runtime, analysis);

		capturedStepLeft.Should().Be(TimeSpan.FromSeconds(SecondStepDuration));
		capturedTotalLeft.Should().Be(TimeSpan.FromSeconds(SecondStepDuration));
	}

	[Fact]
	public void StepTransition_RecalculatesCorrectly()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, FirstStepDuration);
		d.AddWait(1).SetDuration(1, SecondStepDuration);

		var timer = services.GetRequiredService<TimerService>();
		var analysis = facade.CurrentSnapshot;

		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(0, 9f), analysis);

		TimeSpan capturedStepLeft = TimeSpan.Zero;
		TimeSpan capturedTotalLeft = TimeSpan.Zero;

		timer.TimesUpdated += (stepLeft, totalLeft) =>
		{
			capturedStepLeft = stepLeft;
			capturedTotalLeft = totalLeft;
		};

		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(1, 0f), analysis);

		capturedStepLeft.Should().Be(TimeSpan.FromSeconds(SecondStepDuration));
		capturedTotalLeft.Should().Be(TimeSpan.FromSeconds(SecondStepDuration));
	}

	[Fact]
	public void ImmediateAction_StepLeftZero()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, FirstStepDuration);
		d.AddFor(1, 1);

		var timer = services.GetRequiredService<TimerService>();
		TimeSpan capturedStepLeft = TimeSpan.Zero;
		TimeSpan capturedTotalLeft = TimeSpan.Zero;

		timer.TimesUpdated += (stepLeft, totalLeft) =>
		{
			capturedStepLeft = stepLeft;
			capturedTotalLeft = totalLeft;
		};

		var analysis = facade.CurrentSnapshot;
		var runtime = RuntimeSnapshotBuilder.CreateActive(stepIndex: 1, stepElapsed: 0f);

		timer.UpdateRuntime(runtime, analysis);

		capturedStepLeft.Should().Be(TimeSpan.Zero);
		capturedTotalLeft.Should().Be(TimeSpan.Zero);
	}
}
