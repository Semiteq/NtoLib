using System;
using System.Collections.Generic;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;
using NtoLib.Test.MbeTable.Core.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Core.Integration.Timer;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "TimerDynamic")]
public sealed class CoreTimerDynamicTests
{
	private const int FirstStepDuration = 10;
	private const int SecondStepDuration = 5;
	private const int ThirdStepDuration = 3;

	[Fact]
	public void MultipleSteps_TotalLeftDecreasesSmoothly()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, FirstStepDuration);
		d.AddWait(1).SetDuration(1, SecondStepDuration);
		d.AddWait(2).SetDuration(2, ThirdStepDuration);

		var timer = services.GetRequiredService<ITimerService>();
		var analysis = facade.CurrentSnapshot;

		var capturedValues = new List<(TimeSpan stepLeft, TimeSpan totalLeft)>();
		timer.TimesUpdated += (stepLeft, totalLeft) => capturedValues.Add((stepLeft, totalLeft));

		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(0, 9f), analysis);
		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(1, 4f), analysis);
		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(2, 2f), analysis);

		capturedValues.Count.Should().Be(3);

		capturedValues[0].totalLeft.Should().Be(TimeSpan.FromSeconds(9));
		capturedValues[1].totalLeft.Should().Be(TimeSpan.FromSeconds(4));
		capturedValues[2].totalLeft.Should().Be(TimeSpan.FromSeconds(1));

		for (int i = 1; i < capturedValues.Count; i++)
		{
			capturedValues[i].totalLeft.Should().BeLessThan(capturedValues[i - 1].totalLeft);
		}
	}

	[Fact]
	public void FullLoopExecution_TotalLeftReachesNearZero()
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
		var analysis = facade.CurrentSnapshot;

		var capturedValues = new List<TimeSpan>();
		timer.TimesUpdated += (stepLeft, totalLeft) => capturedValues.Add(totalLeft);

		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(1, 4f, for1: 0), analysis);
		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(1, 4f, for1: 1), analysis);
		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(1, 4f, for1: 2), analysis);

		capturedValues.Count.Should().Be(3);

		capturedValues[0].Should().Be(TimeSpan.FromSeconds(11));
		capturedValues[1].Should().Be(TimeSpan.FromSeconds(6));
		capturedValues[2].Should().Be(TimeSpan.FromSeconds(1));

		for (int i = 1; i < capturedValues.Count; i++)
		{
			capturedValues[i].Should().BeLessThan(capturedValues[i - 1]);
		}
	}

	[Fact]
	public void Regression_ThenRecovery_ContinuesCorrectly()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, FirstStepDuration);

		var timer = services.GetRequiredService<ITimerService>();
		var analysis = facade.CurrentSnapshot;

		var capturedValues = new List<TimeSpan>();
		timer.TimesUpdated += (stepLeft, totalLeft) => capturedValues.Add(totalLeft);

		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(0, 5f), analysis);
		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(0, 3f), analysis);
		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(0, 6f), analysis);

		capturedValues.Count.Should().Be(3);

		capturedValues[0].Should().Be(TimeSpan.FromSeconds(5));
		capturedValues[1].Should().Be(TimeSpan.FromSeconds(5));
		capturedValues[2].Should().Be(TimeSpan.FromSeconds(4));
	}

	[Fact]
	public void SmoothProgressionWithinStep_DecreasesLinearly()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0).SetDuration(0, FirstStepDuration);

		var timer = services.GetRequiredService<ITimerService>();
		var analysis = facade.CurrentSnapshot;

		var capturedStepLeft = new List<TimeSpan>();
		timer.TimesUpdated += (stepLeft, totalLeft) => capturedStepLeft.Add(stepLeft);

		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(0, 0f), analysis);
		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(0, 2f), analysis);
		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(0, 5f), analysis);
		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(0, 9f), analysis);

		capturedStepLeft.Count.Should().Be(4);

		capturedStepLeft[0].Should().Be(TimeSpan.FromSeconds(10));
		capturedStepLeft[1].Should().Be(TimeSpan.FromSeconds(8));
		capturedStepLeft[2].Should().Be(TimeSpan.FromSeconds(5));
		capturedStepLeft[3].Should().Be(TimeSpan.FromSeconds(1));

		for (int i = 1; i < capturedStepLeft.Count; i++)
		{
			capturedStepLeft[i].Should().BeLessThan(capturedStepLeft[i - 1]);
		}
	}

	[Fact]
	public void NestedLoop_FullExecution_MonotonicDecrease()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		const int OuterIterations = 2;
		const int InnerIterations = 2;
		const int BodyDuration = 5;

		var d = new RecipeTestDriver(facade);
		d.AddFor(0, OuterIterations);
		d.AddFor(1, InnerIterations);
		d.AddWait(2).SetDuration(2, BodyDuration);
		d.AddEndFor(3);
		d.AddEndFor(4);

		var timer = services.GetRequiredService<ITimerService>();
		var analysis = facade.CurrentSnapshot;

		var capturedValues = new List<TimeSpan>();
		timer.TimesUpdated += (stepLeft, totalLeft) => capturedValues.Add(totalLeft);

		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(2, 0f, for1: 0, for2: 0), analysis);
		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(2, 0f, for1: 0, for2: 1), analysis);
		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(2, 0f, for1: 1, for2: 0), analysis);
		timer.UpdateRuntime(RuntimeSnapshotBuilder.CreateActive(2, 0f, for1: 1, for2: 1), analysis);

		capturedValues.Count.Should().Be(4);

		capturedValues[0].Should().Be(TimeSpan.FromSeconds(20));
		capturedValues[1].Should().Be(TimeSpan.FromSeconds(15));
		capturedValues[2].Should().Be(TimeSpan.FromSeconds(10));
		capturedValues[3].Should().Be(TimeSpan.FromSeconds(5));

		for (int i = 1; i < capturedValues.Count; i++)
		{
			capturedValues[i].Should().BeLessThan(capturedValues[i - 1]);
		}
	}
}
