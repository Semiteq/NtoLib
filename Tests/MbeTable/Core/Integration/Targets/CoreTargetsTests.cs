using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleCore.Services;

using Tests.MbeTable.Core.Helpers;

using Xunit;

namespace Tests.MbeTable.Core.Integration.Targets;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "Targets")]
public sealed class CoreTargetsTests
{
	private const string CloseActionName = "Закрыть";
	private const string TargetColumnKey = "target";
	private const short ExpectedMinimalTargetId = 1;

	[Fact]
	public void Actions_List_NotEmpty()
	{
		var (services, _) = CoreTestHelper.BuildCore();
		using var __ = services as IDisposable;

		var provider = services.GetRequiredService<ComboboxDataProvider>();

		var actions = provider.GetActions();

		actions.Should().NotBeEmpty();
	}

	[Fact]
	public void EnumOptions_ForActionTargetColumn_Succeeds()
	{
		var (services, _) = CoreTestHelper.BuildCore();
		using var __ = services as IDisposable;

		var repo = services.GetRequiredService<ActionRepository>();
		var provider = services.GetRequiredService<ComboboxDataProvider>();

		var id = ActionNameHelper.GetActionIdOrThrow(repo, CloseActionName);
		var result = provider.GetResultEnumOptions(id, TargetColumnKey);

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().ContainKey(ExpectedMinimalTargetId);
	}
}
