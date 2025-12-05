using System;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.MbeTable.ModuleCore.Facade;

namespace NtoLib.Test.MbeTable.Core.Helpers;

public static class CoreTestHelper
{
	public static (IServiceProvider Services, IRecipeFacade Facade) BuildCore()
	{
		var dir = YamlTestDataHelper.PrepareYamlConfigDirectory();
		var provider = new TestConfigProvider(dir);
		var services = TestServiceProviderFactory.Create(provider.AppConfiguration, provider.CompiledFormulas);
		var facade = services.GetRequiredService<IRecipeFacade>();
		return (services, facade);
	}
}
