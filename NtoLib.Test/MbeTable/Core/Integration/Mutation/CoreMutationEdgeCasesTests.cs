using FluentAssertions;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Test.MbeTable.Core.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Core.Integration.Mutation;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "MutationEdgeCases")]
public sealed class CoreMutationEdgeCasesTests
{
	private const int InvalidNegativeIndex = -1;
	private const int InvalidLargeIndex = 100;

	[Fact]
	public void RemoveStep_NegativeIndex_Fails()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0);

		var result = facade.RemoveStep(InvalidNegativeIndex);

		result.IsFailed.Should().BeTrue();
	}

	[Fact]
	public void RemoveStep_IndexBeyondCount_Fails()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0);

		var result = facade.RemoveStep(InvalidLargeIndex);

		result.IsFailed.Should().BeTrue();
	}

	[Fact]
	public void UpdateProperty_NonExistentColumn_Fails()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0);

		var nonExistentColumn = new ColumnIdentifier("non_existent_column");
		var result = facade.UpdateProperty(0, nonExistentColumn, 123f);

		result.IsFailed.Should().BeTrue();
	}

	[Fact]
	public void UpdateProperty_TypeMismatch_Fails()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0);

		var result = facade.UpdateProperty(0, MandatoryColumns.StepDuration, "not_a_valid_number");

		result.IsFailed.Should().BeTrue();
	}
}
