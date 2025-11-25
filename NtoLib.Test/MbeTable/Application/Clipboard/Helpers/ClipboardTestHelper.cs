using System;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ServiceClipboard;

namespace NtoLib.Test.MbeTable.Application.Clipboard.Helpers;

public static class ClipboardTestHelper
{
	public static (IServiceProvider Services, IRecipeApplicationService App, FakeClipboardRawAccess Clipboard)
		BuildApplication()
	{
		var dir = ClipboardYamlHelper.PrepareYamlConfigDirectory();
		var provider = new ClipboardTestConfigProvider(dir);
		var services =
			ApplicationTestServiceProviderFactory.Create(provider.AppConfiguration, provider.CompiledFormulas);
		var app = services.GetRequiredService<IRecipeApplicationService>();
		var clipboard = services.GetRequiredService<IClipboardRawAccess>() as FakeClipboardRawAccess;

		if (clipboard == null)
			throw new InvalidOperationException("Expected FakeClipboardRawAccess in DI container");

		return (services, app, clipboard);
	}
}
