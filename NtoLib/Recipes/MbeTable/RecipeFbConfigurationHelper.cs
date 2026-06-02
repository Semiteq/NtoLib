using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Microsoft.Extensions.Logging.Abstractions;

using NtoLib.Recipes.MbeTable.ModuleConfig;
using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.PinGroups;
using NtoLib.Recipes.MbeTable.ModuleConfig.Formulas;
using NtoLib.Recipes.MbeTable.ModuleCore.Formulas;

namespace NtoLib.Recipes.MbeTable;

/// <summary>
/// COM-neutral configuration and dynamic-pin logic shared by the recipe FB shells
/// (<see cref="MbeTableFB" /> and the editor variant). Keeps the YAML loading, formula
/// precompilation and pin-group reading in one place so both FBs delegate instead of
/// duplicating the bodies.
/// </summary>
internal static class RecipeFbConfigurationHelper
{
	/// <summary>
	/// Holds the loaded configuration together with the formulas precompiled from it.
	/// </summary>
	internal sealed record LoadedConfiguration(
		AppConfiguration Configuration,
		IReadOnlyDictionary<short, CompiledFormula> CompiledFormulas);

	public static LoadedConfiguration LoadConfiguration(
		string configDirPath,
		string propertyDefsFileName,
		string columnDefsFileName,
		string pinGroupDefsFileName,
		string actionsDefsFileName)
	{
		IConfigurationLoader loader = new ConfigurationLoader();

		try
		{
			var config = loader.LoadConfiguration(
				configDirPath,
				propertyDefsFileName,
				columnDefsFileName,
				pinGroupDefsFileName,
				actionsDefsFileName);

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

			return new LoadedConfiguration(config, precompileResult.Value);
		}
		catch (ConfigException ex)
		{
			var fullMessage = BuildConfigExceptionMessage(ex);
			MessageBox.Show(fullMessage, @"Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

			throw;
		}
		catch (Exception ex)
		{
			var fullMessage = $"Unexpected configuration error: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
			MessageBox.Show(fullMessage, @"Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

			throw;
		}
	}

	public static IReadOnlyCollection<string> GetDefinedGroupNames(AppConfiguration configuration)
	{
		return configuration.PinGroupData
			.Select(g => g.GroupName)
			.ToArray();
	}

	/// <summary>
	/// Reads the named pin group. <paramref name="tryReadPin" /> returns the pin's string
	/// value when the pin has good quality, or <c>null</c> when it should be skipped — the
	/// FB owns the platform-specific quality check, keeping this method COM-neutral.
	/// </summary>
	public static IReadOnlyDictionary<int, string> ReadTargets(
		AppConfiguration configuration,
		string groupName,
		Func<int, string?> tryReadPin)
	{
		if (string.IsNullOrWhiteSpace(groupName))
		{
			throw new ArgumentNullException(nameof(groupName));
		}

		var pinGroup = configuration.PinGroupData
			.FirstOrDefault(g => string.Equals(g.GroupName, groupName, StringComparison.OrdinalIgnoreCase));

		return pinGroup == null
			? throw new InvalidOperationException($"Group '{groupName}' is not defined in PinGroupDefs.yaml.")
			: ReadPinGroup(pinGroup.FirstPinId, pinGroup.PinQuantity, tryReadPin);
	}

	public static IReadOnlyDictionary<int, string> ReadPinGroup(
		int firstId,
		int quantity,
		Func<int, string?> tryReadPin)
	{
		const int FirstKey = 1;
		var pinGroup = new Dictionary<int, string>(quantity);

		for (var offset = 0; offset < quantity; offset++)
		{
			var pinId = firstId + offset;
			var value = tryReadPin(pinId);
			if (value == null)
			{
				continue;
			}

			pinGroup[offset + FirstKey] = value;
		}

		return pinGroup;
	}

	public static IEnumerable<(int PinId, string PinName)> EnumerateGroupPins(PinGroupData pinGroup)
	{
		for (var i = 0; i < pinGroup.PinQuantity; i++)
		{
			yield return (pinGroup.FirstPinId + i, $"{pinGroup.GroupName}{i + 1}");
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
