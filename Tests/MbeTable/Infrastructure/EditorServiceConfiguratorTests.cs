using System;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Modbus;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure;
using NtoLib.Recipes.MbeTable.ModulePresentation;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP;
using NtoLib.Recipes.MbeTableEditor;

using Tests.MbeTable.Application.Clipboard.Helpers;
using Tests.MbeTable.Core.Helpers;

using Xunit;

namespace Tests.MbeTable.Infrastructure;

/// <summary>
/// DI regression guard: the editor graph must resolve the presentation surface
/// (<see cref="TableControlServices" />) without any PLC / Modbus service registered.
/// </summary>
public sealed class EditorServiceConfiguratorTests
{
	[Fact]
	public void ConfigureEditorServices_ResolvesTableControlServices_WithoutPlcServices()
	{
		var configProvider = new ClipboardTestConfigProvider(YamlTestDataHelper.PrepareYamlConfigDirectory());
		var editorFb = new MbeTableEditorFB();

		using var provider = (ServiceProvider)MbeTableServiceConfigurator.ConfigureEditorServices(
			editorFb,
			configProvider.AppConfiguration,
			configProvider.CompiledFormulas);

		var tableControlServices = provider.GetRequiredService<TableControlServices>();
		tableControlServices.Should().NotBeNull();

		tableControlServices.RowStateProvider.Should().BeOfType<StaticRowExecutionStateProvider>();

		provider.GetService<IModbusTcpService>().Should().BeNull();
		provider.GetService<RecipePlcService>().Should().BeNull();
		provider.GetService<ModbusTcpService>().Should().BeNull();
	}

	[Fact]
	public void ConfigureEditorServices_RegistersEditorFbAsPinGroupReader()
	{
		var configProvider = new ClipboardTestConfigProvider(YamlTestDataHelper.PrepareYamlConfigDirectory());
		var editorFb = new MbeTableEditorFB();

		using var provider = (ServiceProvider)MbeTableServiceConfigurator.ConfigureEditorServices(
			editorFb,
			configProvider.AppConfiguration,
			configProvider.CompiledFormulas);

		provider.GetRequiredService<NtoLib.Recipes.MbeTable.IPinGroupReader>().Should().BeSameAs(editorFb);
	}
}
