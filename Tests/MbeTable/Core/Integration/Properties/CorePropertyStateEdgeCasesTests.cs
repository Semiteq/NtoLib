using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;

using Tests.MbeTable.Core.Helpers;

using Xunit;

namespace Tests.MbeTable.Core.Integration.Properties;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "PropertyStateEdgeCases")]
public sealed class CorePropertyStateEdgeCasesTests
{
	[Fact]
	public void ReadOnlyColumn_FromColumnDefs_IsReadonly()
	{
		var (services, facade) = CoreTestHelper.BuildCore();
		using var _ = services as IDisposable;

		var d = new RecipeTestDriver(facade);
		d.AddWait(0);

		var provider = services.GetRequiredService<PropertyStateProvider>();
		var step = facade.CurrentSnapshot.Recipe.Steps[0];

		var stepStartTimeState = provider.GetPropertyState(step, MandatoryColumns.StepStartTime);

		stepStartTimeState.Should().Be(PropertyState.Readonly);
	}
}
