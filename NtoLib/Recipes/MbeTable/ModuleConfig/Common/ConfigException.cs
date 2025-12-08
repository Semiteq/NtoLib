using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Common;

/// <summary>
/// Boundary exception that aggregates configuration errors for throwing to the host.
/// </summary>
public sealed class ConfigException : Exception
{
	public IReadOnlyList<ConfigError> Errors { get; }

	public ConfigException(IEnumerable<ConfigError> errors)
		: base(BuildMessage(errors))
	{
		Errors = errors?.ToArray() ?? Array.Empty<ConfigError>();
	}

	private static string BuildMessage(IEnumerable<ConfigError> errors)
	{
		var list = errors?.ToArray() ?? Array.Empty<ConfigError>();
		if (list.Length == 0)
			return "Configuration failed with no details.";

		var sb = new StringBuilder();
		sb.AppendLine("Configuration loading failed with the following errors:");

		foreach (var e in list)
		{
			sb.Append("- ")
				.Append(e.Message);

			if (!string.IsNullOrWhiteSpace(e.Section) || !string.IsNullOrWhiteSpace(e.Context))
			{
				sb.Append(" [")
					.Append(string.IsNullOrWhiteSpace(e.Section) ? "" : $"section={e.Section}")
					.Append(string.IsNullOrWhiteSpace(e.Context)
						? ""
						: (string.IsNullOrWhiteSpace(e.Section) ? "" : ", "))
					.Append(string.IsNullOrWhiteSpace(e.Context) ? "" : $"context={e.Context}")
					.Append(']');
			}

			if (e.Metadata?.Any() == true)
			{
				sb.AppendLine();
				sb.Append("  metadata: ");
				sb.Append(string.Join(", ", e.Metadata.Select(kv => $"{kv.Key}={kv.Value}")));
			}

			sb.AppendLine();
		}

		return sb.ToString();
	}
}
