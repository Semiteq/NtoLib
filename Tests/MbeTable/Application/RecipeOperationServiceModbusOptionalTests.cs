using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;

using Tests.MbeTable.Application.Clipboard.Helpers;

using Xunit;

namespace Tests.MbeTable.Application;

[Trait("Category", "Integration")]
[Trait("Component", "Application")]
[Trait("Area", "RecipeOperationServiceModbusOptional")]
public sealed class RecipeOperationServiceModbusOptionalTests
{
	private static RecipeOperationService BuildWithoutModbus(out IServiceProvider services)
	{
		var dir = ClipboardYamlHelper.PrepareYamlConfigDirectory();
		var provider = new ClipboardTestConfigProvider(dir);
		services = ApplicationTestServiceProviderFactory.Create(
			provider.AppConfiguration,
			provider.CompiledFormulas,
			registerModbus: false);

		return services.GetRequiredService<RecipeOperationService>();
	}

	[Fact]
	public async Task SendRecipeAsync_WhenModbusServiceMissing_ReturnsFailedResult()
	{
		var app = BuildWithoutModbus(out var services);
		using var _ = services as IDisposable;

		var result = await app.SendRecipeAsync();

		result.IsFailed.Should().BeTrue();
		result.Errors.Should().ContainSingle()
			.Which.Should().BeOfType<ApplicationInvalidOperationError>()
			.Which.Details.Should().Be("PLC communication is not available");
	}

	[Fact]
	public async Task ReceiveRecipeAsync_WhenModbusServiceMissing_ReturnsFailedResult()
	{
		var app = BuildWithoutModbus(out var services);
		using var _ = services as IDisposable;

		var result = await app.ReceiveRecipeAsync();

		result.IsFailed.Should().BeTrue();
		result.Errors.Should().ContainSingle()
			.Which.Should().BeOfType<ApplicationInvalidOperationError>()
			.Which.Details.Should().Be("PLC communication is not available");
	}

	[Fact]
	public void EditingOperations_WhenModbusServiceMissing_StillSucceed()
	{
		var app = BuildWithoutModbus(out var services);
		using var _ = services as IDisposable;

		var result = app.AddStep(0);

		result.IsSuccess.Should().BeTrue();
		app.GetRowCount().Should().Be(1);
	}
}
