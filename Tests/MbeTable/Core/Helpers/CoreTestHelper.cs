using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleCore.Facade;

namespace Tests.MbeTable.Core.Helpers;

public static class CoreTestHelper
{
	public static (IServiceProvider Services, RecipeFacade Facade) BuildCore()
	{
		var dir = YamlTestDataHelper.PrepareYamlConfigDirectory();
		var provider = new TestConfigProvider(dir);
		var services = TestServiceProviderFactory.Create(provider.AppConfiguration, provider.CompiledFormulas);
		var facade = services.GetRequiredService<RecipeFacade>();
		return (services, facade);
	}
}
