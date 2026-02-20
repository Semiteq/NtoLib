using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ServiceClipboard;

namespace Tests.MbeTable.Application.Clipboard.Helpers;

public static class ClipboardTestHelper
{
	public static (IServiceProvider Services, RecipeOperationService App, FakeClipboardRawAccess Clipboard)
		BuildApplication()
	{
		var dir = ClipboardYamlHelper.PrepareYamlConfigDirectory();
		var provider = new ClipboardTestConfigProvider(dir);
		var services =
			ApplicationTestServiceProviderFactory.Create(provider.AppConfiguration, provider.CompiledFormulas);
		var app = services.GetRequiredService<RecipeOperationService>();

		if (services.GetRequiredService<IClipboardRawAccess>() is not FakeClipboardRawAccess clipboard)
		{
			throw new InvalidOperationException("Expected FakeClipboardRawAccess in DI container");
		}

		return (services, app, clipboard);
	}
}
