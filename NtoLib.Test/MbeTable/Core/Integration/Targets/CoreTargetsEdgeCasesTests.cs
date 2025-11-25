using System;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Test.MbeTable.Core.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Core.Integration.Targets;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "TargetsEdgeCases")]
public sealed class CoreTargetsEdgeCasesTests
{
	private const string EmptyActionName = "";
	private const string CloseActionName = "Закрыть";
	private const string InvalidColumnKey = "invalid_column";

	[Fact]
	public void GetActionDefinitionByName_EmptyName_Fails()
	{
		var (services, _) = CoreTestHelper.BuildCore();
		using var __ = services as IDisposable;

		var repo = services.GetRequiredService<IActionRepository>();

		var result = repo.GetResultActionDefinitionByName(EmptyActionName);

		result.IsFailed.Should().BeTrue();
	}

	[Fact]
	public void GetActionDefinitionByName_NullName_Fails()
	{
		var (services, _) = CoreTestHelper.BuildCore();
		using var __ = services as IDisposable;

		var repo = services.GetRequiredService<IActionRepository>();

		var result = repo.GetResultActionDefinitionByName(string.Empty);

		result.IsFailed.Should().BeTrue();
	}

	[Fact]
	public void GetEnumOptions_InvalidColumnKey_Fails()
	{
		var (services, _) = CoreTestHelper.BuildCore();
		using var __ = services as IDisposable;

		var repo = services.GetRequiredService<IActionRepository>();
		var provider = services.GetRequiredService<IComboboxDataProvider>();

		var actionId = ActionNameHelper.GetActionIdOrThrow(repo, CloseActionName);
		var result = provider.GetResultEnumOptions(actionId, InvalidColumnKey);

		result.IsFailed.Should().BeTrue();
	}
}
