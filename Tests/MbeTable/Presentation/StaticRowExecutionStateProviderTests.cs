using FluentAssertions;

using NtoLib.Recipes.MbeTable.ModulePresentation.Models;
using NtoLib.Recipes.MbeTableEditor;

using Xunit;

namespace Tests.MbeTable.Presentation;

public sealed class StaticRowExecutionStateProviderTests
{
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(42)]
	public void GetState_ForAnyRow_ReturnsUpcoming(int rowIndex)
	{
		var provider = new StaticRowExecutionStateProvider();

		var state = provider.GetState(rowIndex);

		state.Should().Be(RowExecutionState.Upcoming);
	}

	[Fact]
	public void CurrentLineChanged_IsNeverRaised()
	{
		var provider = new StaticRowExecutionStateProvider();
		var raised = false;
		provider.CurrentLineChanged += (_, _) => raised = true;

		provider.GetState(0);
		provider.GetState(5);

		raised.Should().BeFalse();
	}

	[Fact]
	public void Dispose_DoesNotThrow()
	{
		var provider = new StaticRowExecutionStateProvider();

		var dispose = () => provider.Dispose();

		dispose.Should().NotThrow();
	}
}
