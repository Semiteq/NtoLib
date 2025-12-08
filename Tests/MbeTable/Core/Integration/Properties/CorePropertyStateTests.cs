using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;

using Tests.MbeTable.Core.Helpers;

using Xunit;

namespace Tests.MbeTable.Core.Integration.Properties;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "PropertyState")]
public sealed class CorePropertyStateTests
{
	private const string CloseActionName = "Закрыть";

	[Fact]
	public void StepStartTime_IsReadonly()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0);

		var provider = services.GetRequiredService<PropertyStateProvider>();
		var step = facade.CurrentSnapshot.Recipe.Steps[0];

		provider.GetPropertyState(step, MandatoryColumns.StepStartTime).Should().Be(PropertyState.Readonly);
	}

	[Fact]
	public void UnsupportedColumn_Disabled()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var repo = services.GetRequiredService<IActionRepository>();
		var actionId = ActionNameHelper.GetActionIdOrThrow(repo, CloseActionName);

		var d = new RecipeTestDriver(facade);
		d.AddDefaultStep(0).ReplaceAction(0, actionId);

		var provider = services.GetRequiredService<PropertyStateProvider>();
		var step = facade.CurrentSnapshot.Recipe.Steps[0];

		provider.GetPropertyState(step, new ColumnIdentifier("speed")).Should().Be(PropertyState.Disabled);
	}
}
