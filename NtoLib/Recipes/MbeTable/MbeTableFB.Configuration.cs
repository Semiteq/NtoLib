using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using Microsoft.Extensions.Logging.Abstractions;

using NtoLib.Recipes.MbeTable.ModuleConfig;
using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleConfig.Formulas;

namespace NtoLib.Recipes.MbeTable;

public partial class MbeTableFB
{
	public IReadOnlyCollection<string> GetDefinedGroupNames()
	{
		var state = EnsureConfigurationLoaded();
		return state.PinGroupData
			.Select(g => g.GroupName)
			.ToArray();
	}

	public Dictionary<int, string> ReadTargets(string groupName)
	{
		if (string.IsNullOrWhiteSpace(groupName))
			throw new ArgumentNullException(nameof(groupName));

		var state = EnsureConfigurationLoaded();
		var pinGroup = state.PinGroupData
			.FirstOrDefault(g => string.Equals(g.GroupName, groupName, StringComparison.OrdinalIgnoreCase));

		return pinGroup == null
			? throw new InvalidOperationException($"Group '{groupName}' is not defined in PinGroupDefs.yaml.")
			: ReadPinGroup(pinGroup.FirstPinId, pinGroup.PinQuantity);
	}

	private AppConfiguration EnsureConfigurationLoaded()
	{
		_appConfigurationLazy ??=
			new Lazy<AppConfiguration>(LoadConfigurationInternal, LazyThreadSafetyMode.ExecutionAndPublication);
		return _appConfigurationLazy.Value;
	}

	private AppConfiguration LoadConfigurationInternal()
	{
		// Backwords compatibility:
		if (_configDirPath is null)
		{
			_configDirPath = Path.Combine(AppContext.BaseDirectory, DefaultConfigFolderName);
		}

		IConfigurationLoader loader = new ConfigurationLoader();

		try
		{
			var config = loader.LoadConfiguration(
				_configDirPath,
				DefaultPropertyDefsFileName,
				DefaultColumnDefsFileName,
				DefaultPinGroupDefsFileName,
				DefaultActionsDefsFileName);

			var precompiler = new FormulaPrecompiler(NullLogger<FormulaPrecompiler>.Instance);
			var precompileResult = precompiler.Precompile(config.Actions);

			if (precompileResult.IsFailed)
			{
				var configErrors = precompileResult.Errors
					.Select(e =>
						e as ConfigError ?? new ConfigError(e.Message, "ActionsDefs.yaml", "formula-precompile"))
					.ToList();

				throw new ConfigException(configErrors);
			}

			_compiledFormulas = precompileResult.Value;
			return config;
		}
		catch (ConfigException ex)
		{
			var fullMessage = BuildConfigExceptionMessage(ex);
			MessageBox.Show(fullMessage, "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			throw;
		}
		catch (Exception ex)
		{
			var fullMessage = $"Unexpected configuration error: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
			MessageBox.Show(fullMessage, "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			throw;
		}
	}

	private static string BuildConfigExceptionMessage(ConfigException ex)
	{
		var lines = new List<string> { "Configuration loading failed with the following errors:", "" };

		foreach (var e in ex.Errors)
		{
			lines.Add(
				$"- {e.Message} [{(string.IsNullOrWhiteSpace(e.Section) ? "" : $"section={e.Section}")}{(string.IsNullOrWhiteSpace(e.Section) || !string.IsNullOrWhiteSpace(e.Context) ? "" : ", ")}{(string.IsNullOrWhiteSpace(e.Context) ? "" : $"context={e.Context}")}]");
			if (e.Metadata?.Any() == true)
			{
				lines.Add("  metadata: " + string.Join(", ", e.Metadata.Select(kv => $"{kv.Key}={kv.Value}")));
			}
		}

		return string.Join(Environment.NewLine, lines);
	}
}
