using System;
using System.Collections.Generic;
using System.Threading;

using InSAT.OPC;

using NtoLib.Recipes.MbeTable;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;

namespace NtoLib.Recipes.MbeTableEditor;

public partial class MbeTableEditorFB
{
	public IReadOnlyCollection<string> GetDefinedGroupNames()
	{
		return RecipeFbConfigurationHelper.GetDefinedGroupNames(EnsureConfigurationLoaded());
	}

	public IReadOnlyDictionary<int, string> ReadTargets(string groupName)
	{
		return RecipeFbConfigurationHelper.ReadTargets(EnsureConfigurationLoaded(), groupName, TryReadPinValue);
	}

	private string? TryReadPinValue(int pinId)
	{
		return GetPinQuality(pinId) != OpcQuality.Good ? null : GetPinValue<string>(pinId);
	}

	private AppConfiguration EnsureConfigurationLoaded()
	{
		_appConfigurationLazy ??=
			new Lazy<AppConfiguration>(LoadConfigurationInternal, LazyThreadSafetyMode.ExecutionAndPublication);

		return _appConfigurationLazy.Value;
	}

	private AppConfiguration LoadConfigurationInternal()
	{
		if (string.IsNullOrWhiteSpace(_configDirPath))
		{
			throw new InvalidOperationException(
				"Configuration directory path (ConfigDirPath) is not set. " +
				"Specify the folder containing the YAML configuration files in the block's properties.");
		}

		var loaded = RecipeFbConfigurationHelper.LoadConfiguration(
			_configDirPath!,
			DefaultPropertyDefsFileName,
			DefaultColumnDefsFileName,
			DefaultPinGroupDefsFileName,
			DefaultActionsDefsFileName);

		_compiledFormulas = loaded.CompiledFormulas;

		return loaded.Configuration;
	}
}
