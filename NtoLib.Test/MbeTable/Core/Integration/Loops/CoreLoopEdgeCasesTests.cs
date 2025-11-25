using System;

using FluentAssertions;

using NtoLib.Test.MbeTable.Core.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Core.Integration.Loops;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "LoopsEdgeCases")]
public sealed class CoreLoopEdgeCasesTests
{
	private const int ZeroIterations = 0;
	private const int NegativeIterations = -5;
	private const int NormalizedIterations = 1;

	[Fact]
	public void ZeroIterations_NormalizesToOne()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddFor(0, ZeroIterations);
		d.AddWait(1).SetDuration(1, 5f);
		d.AddEndFor(2);

		var snap = facade.CurrentSnapshot;

		snap.IsValid.Should().BeTrue();
		var loop = snap.LoopTree.ByStartIndex[0];
		loop.EffectiveIterationCount.Should().Be(NormalizedIterations);
	}

	[Fact]
	public void NegativeIterations_NormalizesToOne()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddFor(0, NegativeIterations);
		d.AddWait(1).SetDuration(1, 5f);
		d.AddEndFor(2);

		var snap = facade.CurrentSnapshot;

		snap.IsValid.Should().BeTrue();
		var loop = snap.LoopTree.ByStartIndex[0];
		loop.EffectiveIterationCount.Should().Be(NormalizedIterations);
	}

	[Fact]
	public void NestedLoops_EnclosingOrder_OuterToInner()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		const int OuterIndex = 0;
		const int InnerIndex = 1;
		const int BodyIndex = 2;

		var d = new RecipeTestDriver(facade);
		d.AddFor(OuterIndex, 2);
		d.AddFor(InnerIndex, 2);
		d.AddWait(BodyIndex).SetDuration(BodyIndex, 5f);
		d.AddEndFor(3);
		d.AddEndFor(4);

		var snap = facade.CurrentSnapshot;

		var enclosing = snap.LoopTree.EnclosingLoopsForStep[BodyIndex];
		enclosing.Count.Should().Be(2);
		enclosing[0].StartIndex.Should().Be(OuterIndex);
		enclosing[1].StartIndex.Should().Be(InnerIndex);
	}

	[Fact]
	public void OrphanEndFor_AtStart_Invalidates()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddEndFor(0);

		var snap = facade.CurrentSnapshot;

		snap.IsValid.Should().BeFalse();
		snap.Flags.LoopIntegrityCompromised.Should().BeTrue();
	}
}
