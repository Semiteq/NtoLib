using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using InSAT.OPC;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;

namespace NtoLib.Recipes.MbeTable;

public partial class MbeTableFB
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
		// Backwards compatibility: projects saved before the ConfigDirPath property existed (#67)
		// deserialize with a null field, and a manually blanked property must not reach
		// Path.Combine as a relative path. Both fall back to the pre-#67 location.
		// The explicit null check narrows the compiler's null-state: net48 mscorlib has no
		// [NotNullWhen(false)] on IsNullOrWhiteSpace, so the call alone would not.
		if (_configDirPath is null || string.IsNullOrWhiteSpace(_configDirPath))
		{
			_configDirPath = Path.Combine(AppContext.BaseDirectory, DefaultConfigFolderName);
		}

		var loaded = RecipeFbConfigurationHelper.LoadConfiguration(
			_configDirPath,
			DefaultPropertyDefsFileName,
			DefaultColumnDefsFileName,
			DefaultPinGroupDefsFileName,
			DefaultActionsDefsFileName);

		_compiledFormulas = loaded.CompiledFormulas;

		return loaded.Configuration;
	}
}
